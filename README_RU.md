## UniLoop
`UniLoop` - библиотека для выполнения C# логики напрямую в игровом цикле Unity (`PlayerLoop`) без использования `MonoBehaviour`.

* Update вне MonoBehaviour — работа в чистых C# классах и сервисах без объектов в сцене
* Zero Allocation — при регистрации, выполнении и завершении процесса
* Отсутствие «пиков» нагрузки на всех этапах жизненного цикла задач
* Max Performance — прямая инъекция в PlayerLoop
* Безопасность добавления и остановки задач во время итерации цикла
* Выполнение задач одного типа строго в порядке их регистрации
* Изоляция ошибок — исключение в одной задаче логируется и не останавливает остальные
* Поддержка CancellationToken.
* IDisposable хэндлы для контроля и управления запущенным процессом
* Безопасность в Editor - автоматическая очистка систем при выходе из PlayMode
* Zero-Alloc State - передача параметров в колбэк без замыканий и упаковки

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
// Fluent API: [Timing + Phase] -> [Type] -> [Schedule]
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


// --- 2. Счётный цикл (Counted Loop) ---
// Выполняется заданное число раз, затем вызывает OnCompleted
UniLoop.Update.Loop.Schedule(() => Debug.Log("Tick"), 3)
    .SetOnCompleted(() => Debug.Log("Done"));

// Однократный вызов при ближайшем срабатывании фазы
UniLoop.Update.Loop.ScheduleOnce(() => Debug.Log("Once"));

// Задержка на N кадров: пустое действие и колбэк завершения
UniLoop.Update.Loop.Schedule(EmptyAction, 10).SetOnCompleted(DoWork);


// --- 3. Цикл по условию (While) ---
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
- **`Counted Loop` - счётный цикл.** - Выполняется заданное число раз, затем завершается. `ScheduleOnce` — частный случай с одним выполнением. Если действие бросит исключение, выполнение всё равно засчитывается.
- **`While` - цикл по условию.** - Выполняется пока предикат возвращает `true`. Также можно отменить или завершить.

### Точки выполнения (Timings & Phases)
Выбор конкретного момента в игровом цикле Unity:
- `PlayerLoopTiming` - выбор игрового цикла: Update, FixedUpdate, LateUpdate 
- `PlayerLoopPhase` - в начале или конце цикла: Early/Late

Соответствие свойств Fluent API точкам вставки:

| Свойство | Timing | Phase | Точка вставки в PlayerLoop |
| :--- | :--- | :--- | :--- |
| `UniLoop.FixedUpdate` | FixedUpdate | Early | начало `FixedUpdate` |
| `UniLoop.PostFixedUpdate` | FixedUpdate | Late | конец `FixedUpdate` |
| `UniLoop.Update` | Update | Early | начало `Update` |
| `UniLoop.PostUpdate` | Update | Late | конец `Update` |
| `UniLoop.LateUpdate` | LateUpdate | Early | начало `PreLateUpdate` |
| `UniLoop.PostLateUpdate` | LateUpdate | Late | конец `PreLateUpdate` |
  
### Порядок выполнения
Очередность выполнения типов операций в пределах одной фазы. Задачи разных типов выполняются группами в этом порядке, а не в порядке вызова `Schedule`:
- Slim Loop - Легкий цикл. Отмена во время итерации применяется со следующего кадра, поэтому задача может выполниться ещё раз в текущем кадре.
- Loop - Бесконечный цикл.
- Loop (with CancellationToken) - Бесконечный цикл с поддержкой CancellationToken.
- Counted Loop - Счётный цикл.
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

- Для цикла по условию (While) и счётного цикла (Counted Loop)
  - `WhileHandle` — хэндл с методом `.Dispose()` для ручной остановки.
  - `TokenWhileHandle` - для процессов с `CancellationToken`
Поддержка подписки на окончание процесса по условию через `.SetOnCompleted()`.

`IsDisposed` становится `true` сразу после `Dispose()`. Сама задача удаляется из цикла в следующем кадре, тогда же вызывается колбэк отмены.

У `TokenLoopHandle` и `TokenWhileHandle` нет `Dispose()`: задачу останавливает токен. Свойство `IsActive` показывает, работает ли задача, и становится `false` сразу после отмены токена.

### Ограничения
- Требуется Unity 2021.3 или новее.
- API вызывается только из главного потока и только в Play Mode. В редакторе и development-сборках нарушение приводит к `InvalidOperationException`.
- При выходе из Play Mode все запланированные задачи отбрасываются без вызова `onCanceled`.



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
        if (_modifiers.Count == 1)
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
- Ограничение: если вызвать `Dispose()` во время выполнения цикла, задача может выполниться ещё раз в текущем кадре. Со следующего кадра она уже не выполняется.
- Регистрация через `UniLoop.Update.Loop.ScheduleSlim(action)` или `UniLoop.ScheduleSlim(...)`.

### Zero-Alloc & Delegate Caching
Использование кэшированных делегатов и State исключает создание замыканий при регистрации процессов для достижения 0B GC Alloc. State должен быть ссылочным типом (`where T : class`), поэтому упаковки тоже нет.

``` cs
public sealed class MyClass
{
    // Кэшируем ссылки на методы один раз при инициализации
    private readonly Func<bool> _cachedPredicate;
    private readonly Action<GameObject> _cachedOnCompleted;
    private IObjectPool<GameObject> _pool;

    public MyClass()
    {
        _cachedPredicate = Predicate;
        _cachedOnCompleted = OnCompleted;
    }

    public void Run()
    {
        var obj = _pool.Get();
        // Передача объекта через State — без аллокаций
        UniLoop.Update.While.Schedule(_cachedPredicate)
            .SetOnCompleted(_cachedOnCompleted, obj);
    }

    private void OnCompleted(GameObject entity) => _pool.Release(entity);
    private bool Predicate() => /* logic */ true;
}
```
### DeferredDenseArray
Специализированная коллекция, лежащая в основе UniLoop.
- Dense Packing: Все активные задачи хранятся в памяти без «дыр». Несмотря на использование ссылочных типов, это гарантирует строгий порядок выполнения и максимально быструю итерацию по списку.
- Deferred Updates: добавление и удаление задач буферизируются и применяются только в безопасные моменты. Это гарантирует стабильность итерации без ошибок изменения коллекции.



## Производительность
Каждый метод выполняет одну и ту же работу в `Update`. Значения — медианы. **Пустой кадр** — кадр без задач, с ним и нужно сравнивать время кадра. **GC** — выделения памяти сверх пустого кадра.

Условия: Unity 6000.3, редактор, Mono (Code Optimization: Debug), Ryzen 5 5600. В Release и в сборке плеера абсолютные значения меньше. Бенчмарки лежат в `UniLoop Tests/Benchmarks`, их можно запустить самостоятельно через Test Runner.

### 10 000 задач: запуск, выполнение и остановка разом

| Метод | Запуск, мс | GC при запуске | Кадр, мс | GC за кадр | Остановка*, мс |
| :--- | ---: | ---: | ---: | ---: | ---: |
| *Пустой кадр* | — | — | *4,01* | — | — |
| **UniLoop Slim** | 1,40 | **0** | 4,29 | **0** | 0,5 |
| **UniLoop Loop** | 2,78 | **0** | 4,23 | **0** | 0,4 |
| **UniLoop Loop + CancellationToken** | 2,85 | **0** | 4,28 | **0** | 0,5 |
| **UniLoop While** | 3,51 | **0** | 4,08 | **0** | 0,5 |
| Свой менеджер (`List<Action>` в `Update`) | 0,21 | 0 | 4,23 | 0 | 0,1 |
| Coroutine | 13,96 | 547 КБ | 8,30 | 0 | 394 |
| UniTask.Yield | 8,13 | 1,8 МБ | 14,49 | 234 КБ | 4,9 |

*\*Остановка — сам вызов плюс отложенная работа в следующем кадре: удаление из списков, обработка отмены.*

### Конвейер: 1000 новых задач каждый кадр, жизнь 10 кадров (~10 000 активных)

**Задачи останавливаются вручную, поштучно**

| Метод | Запуск и отмена за кадр, мс | Кадр, мс | GC за кадр |
| :--- | ---: | ---: | ---: |
| *Пустой кадр* | — | *4,07* | — |
| **UniLoop Loop** | 0,37 | 4,25 | **0** |
| Свой менеджер | 0,06 | 4,07 | 0 |
| Coroutine | 79,03 | 10,27 | 56 КБ |
| UniTask.Yield (токен на задачу) | 1,18 | 14,57 | 688 КБ |

**Задачи завершаются сами**

| Метод | Запуск за кадр, мс | Кадр, мс | GC за кадр |
| :--- | ---: | ---: | ---: |
| *Пустой кадр* | — | *3,93* | — |
| **UniLoop Counted** | 0,42 | 4,35 | **0** |
| **UniLoop While** (счётчик в замыкании) | 0,44 | 4,63 | 145 КБ |
| Свой менеджер | 0,04 | 4,18 | 0 |
| Coroutine | 1,37 | 9,14 | 63 КБ |
| UniTask.Yield | 0,39 | 6,07 | 47 КБ |

### Итоги
- **Без выделений памяти** на всех этапах: запуск, выполнение, отмена, завершение. Корутины и UniTask выделяют от десятков килобайт до мегабайт.
- **Выполнение** 10 000 задач почти не отличается от пустого кадра. Корутины добавляют около 4 мс, UniTask — около 10 мс.
- **Стабильная стоимость.** UniLoop стоит одинаково, отменяются задачи вручную или завершаются сами: около 0,4 мс на 1000 операций за кадр. У корутин поштучная остановка обходится в 79 мс за кадр, у UniTask с токеном на задачу — в +10 мс к кадру и 688 КБ.
- **While с замыканием** выделяет память на каждую задачу: это замыкание в пользовательском коде. Для «выполнить N раз» используйте счётный цикл — он не выделяет ничего.

### Почему не просто `List<Action>`?
Свой менеджер на `List<Action>` быстрее на запуске и остановке, но умеет только перебирать список. UniLoop добавляет к этому:

- **Безопасные изменения во время цикла** — задачу можно запустить или остановить прямо из другой задачи.
- **Защищённые хэндлы** — проверка по поколению: повторный `Dispose` или устаревший хэндл ничего не сломают.
- **Изоляцию ошибок** — исключение в одной задаче не останавливает остальные.
- **Колбэки и отмену** — `SetOnCompleted`, `SetOnCanceled`, `CancellationToken`, задачи «пока условие» и «N раз».
- **Любую точку кадра** — начало и конец `Update`, `FixedUpdate` и `LateUpdate` без лишних MonoBehaviour.
- **Состояние без замыканий** — данные передаются в колбэк без выделения памяти.

Цена — десятые доли миллисекунды на тысячу операций за кадр, в обычной игре это микросекунды.


## Установка

Вы можете установить **UniLoop** через Unity Package Manager (UPM), используя Git URL.

1. Откройте окно **Package Manager** (`Window` -> `Package Manager`).
2. Нажмите на иконку **"+"** в левом верхнем углу.
3. Выберите **"Add package from git URL..."**.
4. Вставьте следующую строку:

`https://github.com/CatCodeGames/UniLoop.git?path=Assets/UniLoop`

## Лицензия
Этот проект распространяется под лицензией **MIT**.
