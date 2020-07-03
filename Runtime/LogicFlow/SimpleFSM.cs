using Transient.SimpleContainer;

namespace Transient.ControlFlow {
    public class SimpleFSM {
        private readonly Dictionary<int, State> _states;
        public State StartState { get; private set; }
        public State EndState { get; private set; }
        public State AnyState { get; private set; }
        public State CurrentState { get; private set; }
        public bool IsAtEnd => CurrentState == EndState;

        public SimpleFSM() {
            const int StateIdStart = -1;
            const int StateIdEnd = -2;
            const int StateIdAny = -3;
            _states = new Dictionary<int, State>(16);
            StartState = new State(StateIdStart, this, StateTransitMode.Automatic);
            EndState = new State(StateIdEnd, this, StateTransitMode.Manual);
            AnyState = new State(StateIdAny, this, StateTransitMode.Manual);
            _states.Add(StateIdStart, StartState);
            _states.Add(StateIdEnd, EndState);
            _states.Add(StateIdAny, AnyState);
            CurrentState = StartState;
            StartState.OnEnter();
        }

        public bool IsInState(int id_) => CurrentState.Id == id_;

        private void Transit(Transition trans_) {
            CurrentState.OnExit();
            CurrentState = trans_.Target;
            CurrentState.OnEnter();
        }

        public void DoTransition(Transition trans_) {
            if (trans_.Source == AnyState || trans_.Source == CurrentState) {
                Transit(trans_);
            }
        }

        public void OnTick(float dt_) {
            CurrentState.OnTick(dt_);
        }

        public void Reset() {
            CurrentState = StartState;
            foreach (var state in _states) {
                state.Value.Reset();
            }
        }

        public State AddState(StateTransitMode mode_ = StateTransitMode.Automatic) {
            var state = new State(_states.Count, this, mode_);
            _states.Add(state.Id, state);
            return state;
        }

        public Transition AddTransition(State source_, State target_, TransitMode mode_ = TransitMode.ActionResult) {
            var trans = new Transition(source_, target_, mode_);
            source_.AddTransition(trans);
            return trans;
        }
    }

    public enum StateTransitMode {
        Automatic,
        Manual,
    }

    public class State {
        public int Id { get; private set; }

        private readonly SimpleFSM _fsm;
        private readonly List<Transition> _transitions;
        public StateTransitMode _mode;
        private bool _isDone;
        private Func<float, bool> _OnTick;
        private Action _OnEnter;
        private Action _OnExit;
        private static readonly Action _DefaultEnter = () => { };
        private static readonly Action _DefaultExit = () => { };

        private State() {
        }

        public State(int id_, SimpleFSM fsm_, StateTransitMode mode_) {
            Id = id_;
            _fsm = fsm_;
            _transitions = new List<Transition>(2);
            _mode = mode_;
        }

        public void AddTransition(Transition trans) => _transitions.Add(trans);

        public void WhenTick(Func<float, bool> OnTick_) {
            _OnTick = OnTick_;
            _isDone = _OnTick == null;
        }

        public void WhenEnter(Action OnEnter_) => _OnEnter = OnEnter_ ?? _DefaultEnter;

        public void WhenExit(Action OnExit_) => _OnExit = OnExit_ ?? _DefaultExit;

        public void Reset() {
            _isDone = _OnTick == null;
        }

        public void OnEnter() {
            Reset();
            _OnEnter?.Invoke();
        }

        public void OnTick(float dt_) {
            if (!_isDone) {
                _isDone = _OnTick(dt_);
            }
            if (_mode == StateTransitMode.Manual || _fsm.CurrentState != this) {
                //never auto transit || external transition happend
                return;
            }
            foreach (var trans in _transitions) {
                if (trans.CanTransit(_isDone)) {
                    _fsm.DoTransition(trans);
                    return;
                }
            }
        }

        public void OnExit() {
            _OnExit?.Invoke();
        }
    }

    public enum TransitMode {
        PassThrough = 0,
        ActionResult,
    }

    public class Transition {
        public State Source { get; private set; }
        public State Target { get; private set; }

        private Func<bool, bool> _Condition;
        private static readonly Func<bool, bool> _DefaultActionResultCondition = c => c;
        private static readonly Func<bool, bool> _DefaultPassThroughCondition = c => true;
        private readonly TransitMode _mode;

        private Transition() {
        }

        public Transition(State source_, State target_, TransitMode mode_) {
            _mode = mode_;
            Source = source_;
            Target = target_;
            _Condition = DefaultCondition();
        }

        private Func<bool, bool> DefaultCondition() {
            if (_mode == TransitMode.ActionResult) {
                return _DefaultActionResultCondition;
            }
            //TransitionMode.PassThrough
            return _DefaultPassThroughCondition;
        }

        public void TransitCondition(Func<bool, bool> Condition_) => _Condition = Condition_ ?? DefaultCondition();

        public bool CanTransit(bool stateIsDone_) => _Condition(stateIsDone_);
    }
}
