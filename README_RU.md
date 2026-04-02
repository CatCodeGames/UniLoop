## UniLoop
`UniLoop` - библиотека для выполнения C# логики напрямую в игровом цикле Unity (`PlayerLoop`) без использования `MonoBehaviour`.

* Update вне MonoBehaviour — работа в чистых C# классах и сервисах без объектов в сцене
* Zero Allocation — при регистрации, выполнении и завершении процесса
* Max Performance — прямая инъекция в PlayerLoop.
* Безопасность добавления и остановки задач во время итерации цикла
* Выполнение задач строго в порядке их регистрации
* Поддержка CancellationToken.
* IDisposable хэндлы для контроля и управления запущенным процессом
* Безопасность в Editor - автоматическая очистка систем при выходе из PlayMode
* Zero-Alloc State - передача параметров в колбек без замканий и упаковки.

### От автора ( или Зачем это всё?)
<details>
<summary> Развернуть </summary>
    
Данная библиотека не пытается заменить Update или обогнать его по скорости. Она появилась по простому списку причин:  
- Лишние MonoBehaviour: Надоело превращать обычные C# классы в MonoBehaviour или тянуть их внутрь только ради одного цикла.
- Цикл без посредников: Синглтоны в сцене — решение стандартное, но зачем стучаться в игровой цикл через посредника, если есть прямой доступ?
- Удобство управления: UniTask.Yield решает задачу, но им не всегда удобно управлять, да и по эффективности он уступает.

В **Unreal Engine** можно запускать логику в цикле без привязки к объектам — и мне это кажется логичным. В `UniLoop` я постарался совместить подход **без аллокаций** от **UniTask** и удобство **Fluent API** от **DOTween**. 

P.S. Внутри библиотеки есть несколько интересных решений, например, специализированные коллекции и работа через хэндлы. Буду рад, если они пригодятся вам отдельно в ваших проектах. Надеюсь, `UniLoop` немного упростит вам жизнь! :)
</details>



## Оглавление
- [Использование](#использование)
- [Устройство и API](#устройство-и-api)
- [Showcase](#showcase)
- [Продвинутая оптимизация](#продвинутая-оптимизация)
- [Производительность](#производительность)
- [Установка](#установка)
- [Лицензия](#лицензия)




## Использование
```cs

// --- 1. Бесконечный цикл (Loop) ---
// Fluent API: [Timing] -> [Phase] -> [Type] -> [Schedule]
LoopHandle handle = UniLoop.Update.Loop.Schedule(() => Debug.Log("Tick"));
handle.Dispose(); // Остановка процесса через дескриптор (handle)

// Регистрация через универсальный метод Schedule
LoopHandle handle = UniLoop.Schedule(() => Debug.Log("Tick"), PlayerLoopTiming.Update, PlayerLoopPhase.Early);

// Добавление колбэка при остановке процесса
LoopHandle handle = UniLoop.FixedUpdate.Loop.Schedule(() => Debug.Log("Tick"))
    .SetOnCanceled(() => Debug.Log("Canceled"));

// Регистрация с отменой по CancellationToken
UniLoop.Update.Loop.Schedule(() => Debug.Log("Tick"), _cts.Token);
_cts.Cancel();


// --- 2. Цикл по условию (While) ---
// Выполняется, пока предикат возвращает true
WhileHandle handle = UniLoop.Update.While.Schedule(() => 
    {
        _interval -= Time.deltaTime;
        return _interval > 0;
    });

// Регистрация While с поддержкой событий завершения и отмены
UniLoop.Update.While.Schedule(predicateFunc, _cts.Token)
    .SetOnCanceled(() => Debug.Log("Canceled"))    // При отмене/Dispose
    .SetOnCompleted(() => Debug.Log("Completed")); // Когда условие стало false

// Использование using гарантирует остановку при выходе из области видимости (scope)
using (var handle = UniLoop.Update.Loop.Schedule(() => Debug.Log("Tick")))
{    
    await DoSomethingAsync();
}
```



## Устройство и API
`UniLoop` — единая точка входа для запуска логики в PlayerLoop через комбинацию **Таймингов**, **Фаз** и **Типов** процессов.


### Типы Процессов:
- **`Loop` - бесконечный цикл.** - Выполняется каждый кадр до ручной остановки или отмены токеном
- **`While` - цикл по условию.** - Выполняется пока предикат возвращает `true`. Также можно отменить или завершить.

### Точки выполнения (Timings & Phases)
Выбор конкретного момента в игровом цикле Unity:
- `PlayerLoopTiming` - выбор игрового цикла: Update, FixedUpdate, LateUpdate 
- `PlayerLoopPhase` - в начале или конце цикла: Early/Late

### Способы запуска 

- **Fluent API**: — последовательный выбор через цепочку:   
```cs
UniLoop.Update.Loop.Schedule(() => ...)
```
- **Универсальный метод `Schedule`** - запуск через передачу параметров `PlayerLoopTiming` и `PlayerLoopPhase`.

### Хэндлы и управление процессами
Каждый запуск возвращает struct handle — легковесный дескриптор для контроля процесса.
- Для бесконечного цикла (Loop)
  - `LoopHandle` - хэндл с методом `.Dispose()` для ручной остановки.
  - `TokenLoopHandle` - для процессов с `CancellationToken`
Оба варианта поддерживают регистрацию колбэка отмены через `.SetOnCanceled()`

- Для цикла по условию (While)
  - `WhileHandle` — хэндл с методом `.Dispose()` для ручной остановки.
  - `TokenWhileHandle` - для процессов с `CancellationToken`
Поддержка подписки на окончание процесса по условию через `.SetOnCompleted()`.



## Showcase
Пример контроллера TimeScale, который включает процесс расчёта множителя только при появлении первого модификатора и полностью отключает его, как только список пустеет.
<details>
  <summary>Примечание</summary>
Код намеренно упрощен для демонстрации управления жизненным циклом через UniLoop.
</details>    
    
```cs
public class TimeScaleController
{
    private readonly List<ITimeScaleModifier> _modifiers = new();
    private readonly float originalTimeScale;
    private LoopHandle _loopHandle;

    public TimeScaleController() => originalTimeScale = Time.timeScale;

    public void AddModifier(ITimeScaleModifier modifier)
    {
        _modifiers.Add(modifier);
            
        // Если это первый модификатор — запускаем логику в цикле
        if (_modifiers.Count != 0)
        {
            _loopHandle = UniLoop.Schedule(UpdateTimeScale, PlayerLoopTiming.Update, PlayerLoopPhase.Early)
                .SetOnCanceled(() => Time.timeScale = originalTimeScale);
        }
    }

    public void RemoveModifier(ITimeScaleModifier modifier)
    {
        if (_modifiers.Remove(modifier) && _modifiers.Count == 0)
        {
            // Если модификаторов не осталось — останавливаем цикл
            _loopHandle.Dispose();
        }
    }

    private void UpdateTimeScale()
    {
        // Вычисляем среднее значение на основе динамических данных от модификаторов
        var averageMultiplier = _modifiers.Average(q => q.Multiplier);
        Time.timeScale = originalTimeScale * averageMultiplier;
    }
}
```



## Продвинутая оптимизация 
### Slim Loop
Облегченная версия цикла. Идеально для глобальных систем, работающих на протяжении всей жизни приложения.
- Самый быстрый способ итерации в PlayerLoop.
- Ограничение: При вызове `Dispose()` внутри выполнения, задача удалится только в следующем кадре.
- Регистрация через `UniLoop.Update.Loop.ScheduleSlim(action)` или `UniLoop.ScheduleSlim(...)`.

### Zero-Alloc & Delegate Caching
Использование кэшированных делегатов и State (структур) исключает создание замыканий и boxing при регистрации процессов для достижения 0B GC Alloc.

``` cs
public sealed class MyClass
{
    private readonly struct PoolContext // Контекст как readonly структура
    {
        public readonly IObjectPool<GameObject> Pool;
        public readonly GameObject Entity;
        public PoolContext(IObjectPool<GameObject> pool, GameObject entity) => (Pool, Entity) = (pool, entity);
    }

    // Кэшируем ссылки на методы один раз при инициализации
    private readonly Func<bool> _cachedPredicate;
    private readonly Action<PoolContext> _cachedOnCompleted;
    private IObjectPool<GameObject> _pool;

    public MyClass()
    {
        _cachedPredicate = Predicate;
        _cachedOnCompleted = OnCompleted;
    }

    public void Run()
    {
        var obj = _pool.Get();
        // Передача PoolContext через State — без аллокаций
        UniLoop.Update.While.Schedule(_cachedPredicate)
            .SetOnCompleted(_cachedOnCompleted, new PoolContext(_pool, obj));
    }

    private void OnCompleted(PoolContext ctx) => ctx.Pool.Release(ctx.Entity);
    private bool Predicate() => /* logic */ true;
}
```
### DeferredDenseArray
Специализированная коллекция, лежащая в основе UniLoop.
- Dense Packing: Все активные задачи хранятся в памяти без «дыр». Несмотря на использование ссылочных типов, это гарантирует строгий порядок выполнения и максимально быструю итерацию по списку.
- Deferred Updates: добавление и удаление задач буферизируются и применяются только в безопасные моменты. Это гарантирует стабильность итерации без ошибок изменения коллекции.



## Производительность
### Бесконечный цикл (Loop)
Сравнение при обработке 10,000 активных задач. За эталон (100%) принят один MonoBehaviour с циклом foreach по HashSet.
|                    | Время (ms) | Скорость (%) |
|--------------------|-----------:|-------------:|
| **UniLoop (Slim)**     | **2.70**       | **157%**         |
| foreach в Update   | 4.23       | 100%         |
| **UniLoop (Default)**  | **4.36**       | **97%**          |
| Coroutines         | 10.44      | 40%          |
| UniTask (Yield)    | 23.54      | 18%          |


<details>
  <summary>Пояснение к тесту</summary> 
    
  - `Update`: Использовался MonoBehaviour, хранящий `Action` в `HashSet` (для простоты удаления). Добавление и удаление новых Action также отложенное.
  - `Coroutine`: Для каждого процесса запущена корутина с `yield return null`.
  - `UniTask`: Бесконечный цикл с использованием `UniTask.Yield`.
</details>

### Цикл по условию (While)
Каждый кадр запускалось по 100 новых процессов (время жизни ~1 сек).

- Регистрация процесса

|                   | Создание (ms) | Скорость (%) | Alloc (KB)  |
|-------------------|--------------:|-------------:|-------------------:|
| foreach в Update  | 0.31    	    | 100%         | 28.1               |
| **UniLoop (Default)** | **0.64**          | **47%**          | **28.1**         |
| Coroutine         | 0.67          | 45%          | 36.7               |
| **UniLoop (NoAlloc)**| **0.88**      | **35%**      | **0**        |
| UniTask (Yield)   | 0.94          | 32%          | 35.9               |
| UniTaskWaitUntil  | 1.1           | 28%          | 37.5               |

- Выполнение

|                   | Выполнение (ms) | Скорость (%) | Alloc (KB) |
|-------------------|-----------------:|-------------:|----------------:|
| foreach в Update  | 2.02             | 100%         | 0             |
| **UniLoop (Default)** | 2.05          | 99%          | 0          |
| **UniLoop (NoAlloc)** | 2.05          | 99%          | 0          |
| WaitUntil 	   | 2.12 	         |95%		       | 0 	       |
| Coroutine         | 3.18             | 63%          | 0             |
| UniTask (Yield)   | 6.49             | 31%          | 0             |

<details>
  <summary>Пояснение к тесту</summary> 
  Каждому процессу передавался временный объект и два метода: предикат и колбэк завершения для возврат объекта в пул.
`UniLoop` (NoAlloc) поддерживает передачу произвольного состояния (Generic State Support), что исключает замыкания (closures) и упаковку (boxing).
  В сочетании с предварительно закэшированными ссылками на методы, это обеспечивает полное отсутствие аллокаций в горячем цикле выполнения.
</details>

### Честный итог
- Скорость: UniLoop сопоставим с нативным циклом и значительно быстрее корутин или UniTask при массовых задачах.
- Регистрация: Требует больше ресурсов CPU, чем простое добавление в Hashset. В основном за счёт использования внутренних пулов.
- Что взамен: 
  - Гарантированный порядок
  - Управление через IDisposable или CancellationToken
  - Возможность работать полностью без аллокаций

## Установка

Вы можете установить **UniLoop** через Unity Package Manager (UPM), используя Git URL.

1. Откройте окно **Package Manager** (`Window` -> `Package Manager`).
2. Нажмите на иконку **"+"** в левом верхнем углу.
3. Выберите **"Add package from git URL..."**.
4. Вставьте следующую строку:

`https://github.com/CatCodeGames/UniLoop.git?path=Assets/PlayerLoop`

## Лицензия
Этот проект распространяется под лицензией **MIT**.
