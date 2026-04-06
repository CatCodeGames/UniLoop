## UniLoop
`UniLoop` - библиотека для выполнения C# логики напрямую в игровом цикле Unity (`PlayerLoop`) без использования `MonoBehaviour`.

* Update вне MonoBehaviour — работа в чистых C# классах и сервисах без объектов в сцене
* Zero Allocation — при регистрации, выполнении и завершении процесса
* Отсутствие «пиков» нагрузки на всех этапах жизненного цикла задач
* Max Performance — прямая инъекция в PlayerLoop
* Безопасность добавления и остановки задач во время итерации цикла
* Выполнение задач строго в порядке их регистрации
* Поддержка CancellationToken.
* IDisposable хэндлы для контроля и управления запущенным процессом
* Безопасность в Editor - автоматическая очистка систем при выходе из PlayMode
* Zero-Alloc State - передача параметров в колбек без замканий и упаковки

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
  
### Порядок выполнения
Очередность выполнения типов операций в пределах одной фазы:
- Slim Loop - Легкий цикл. В отличие от остальных типов, модификация коллекции (добавление/удаление) во время итерации не безопасна.
- Loop - Бесконечный цикл.
- Loop (with CancellationToken) - Бесконечный цикл с поддержкой CancellationToken.
- While - Цикл по условию.
- While (with CancellationToken) - Цикл по условию с поддержкой CancellationToken.
  
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
Сценарии с постоянным созданием и завершением задач каждый кадр (конвейерная нагрузка).

#### Сценарий А: 100 новых задач / кадр (время жизни 100 кадров, ~10,000 активных)

| Метод | Запуск (ms) | Выполнение (ms) | Отмена (ms) | GC Alloc |
| :--- | :---: | :---: | :---: | :---: |
| **UniLoop (No Alloc)** | **0.10** | **0.55** | **4.31** | **0 B** |
| UniLoop (Default) | 0.11 | 0.58 | 3.59 | 28.1 KB |
| Update (Baseline) | 0.07 | 0.59 | 3.25 | 28.1 KB |
| UniTask.Yield | 0.15 | 2.62 | 2.62 | 35.9 KB |
| Coroutine | 0.34 | 6.04 | 6.04 | 36.7 KB |
| UniTask.WaitUntil | 0.17 | 0.85 | 85.70 | 4.5 MB |

#### Сценарий Б: 1000 новых задач / кадр (время жизни 10 кадров, ~10,000 активных)

| Метод | Запуск (ms) | Выполнение (ms) | Отмена (ms) | GC Alloc |
| :--- | :---: | :---: | :---: | :---: |
| **UniLoop (No Alloc)** | **0.73** | **0.93** | **4.50** | **0 B** |
| UniLoop (Default) | 0.80 | 0.93 | 3.29 | 281.2 KB |
| Update (Baseline) | 0.55 | 1.44 | 3.49 | 281.2 KB |
| UniTask.Yield | 1.43 | 3.17 | 3.17 | 359.4 KB |
| Coroutine | 2.23 | 6.17 | 6.17 | 367.2 KB |
| UniTask.WaitUntil | 1.70 | 2.21 | 90.02 | 4.9 MB |

> **Важно:** Обратите внимание на `UniTask.WaitUntil`. При массовой отмене задач он генерирует лавину исключений, что приводит к критическим скачкам аллокаций (**4.9 MB**) и фризам (**90 ms**). UniLoop в режиме **No Alloc** сохраняет стабильный FPS и нулевое выделение памяти.

### Итоги
- **Регистрация** — по затратам CPU сопоставима с добавлением в стандартные коллекции и запуском UniTask. Поддержка режима No Alloc позволяет полностью исключить нагрузку на память при создании задач.
- **Выполнение** — в динамических сценариях (постоянный приток и завершение задач) превосходит стандартную итерацию (foreach) и асинхронные методы. Использование Slim Loop позволяет сократить время выполнения в несколько раз относительно Update.
- **Завершение и отмена** — минимальное влияние на производительность кадра. В отличие от асинхронных решений, система сохраняет стабильный FPS и отсутствие аллокаций даже при одновременной остановке тысяч задач.

####Ключевые преимущества:

- Гарантированный порядок выполнения всех операций в кадре.
- Поддержка Zero-Alloc: возможность работы без аллокаций на протяжении всего жизненного цикла задачи.
- Гибкий контроль через IDisposable хэндлы или CancellationToken.
- Отсутствие «пиков» нагрузки на всех этапах жизненного цикла задач.


## Установка

Вы можете установить **UniLoop** через Unity Package Manager (UPM), используя Git URL.

1. Откройте окно **Package Manager** (`Window` -> `Package Manager`).
2. Нажмите на иконку **"+"** в левом верхнем углу.
3. Выберите **"Add package from git URL..."**.
4. Вставьте следующую строку:

`https://github.com/CatCodeGames/UniLoop.git?path=Assets/PlayerLoop`

## Лицензия
Этот проект распространяется под лицензией **MIT**.
