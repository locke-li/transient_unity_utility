using Transient;
using Transient.SimpleContainer;

using Enumerator = System.Collections.Generic.IEnumerator<CoroutineState>;

public enum CoroutineState {
    Init,
    Waiting,
    Executing,
    Done
}

internal struct CoroutineCache {
    public Enumerator enumerator;
    public Action OnExecuteDone;

    public CoroutineState Step() {
        if (enumerator.MoveNext()) {
            return enumerator.Current;
        }
        return CoroutineState.Done;
    }
}

public class GeneratorCoroutine {
    private List<CoroutineCache> CoroutineList { get; set; } = new List<CoroutineCache>(16);

    public GeneratorCoroutine(ActionList<float> Owner) {
        Owner.Add(Update, this);
    }

    public void Execute<P>(Func<P, Enumerator> routine_, P parameter_, Action OnExecuteDone_ = null) {
        CoroutineList.Add(new CoroutineCache() {
            enumerator = routine_(parameter_),
            //can be null
            OnExecuteDone = OnExecuteDone_
        });
    }

    private void Update(float deltaTime) {
        for (int r = 0; r < CoroutineList.Count; ++r) {
            var state = CoroutineList[r].Step();
            if (state == CoroutineState.Done) {
                CoroutineList[r].OnExecuteDone?.Invoke();
                CoroutineList.OutOfOrderRemoveAt(r);
                --r;
            }
        }
    }
}
