using System;
using Transient.SimpleContainer;

namespace Transient.ControlFlow {
    public class SimpleFSM {
        public SimpleFSMGraph Graph { get; set; }
        public State CurrentState { get; private set; }
        internal object[] Token { get; private set; }
        private bool _isDone;
        public Action<int, int, int> WhenTransit { get; set; }
        private int transitDepth = 0;
        public static int MaxTransitDepth = 16;

        public bool IsInState(int id_) => CurrentState.Id == id_;

        public void Init(SimpleFSMGraph graph_, params object[] token_) {
            Graph = graph_;
            Token = token_ ?? new object[1];
        }

        public void Reset() {
            TransitTo(Graph[0]);
        }

        public void ForceState(int id_) {
            if (id_ < 0) {
                Log.Warning($"invalid state {id_}");
                return;
            }
            CurrentState = Graph[id_];
        }


        internal void Error(Exception e_) {
            Log.Error($"{e_.Message}\n{e_.StackTrace}");
            CurrentState = SimpleFSMGraph.ErrorState;
            CurrentState.OnEnter(this, false);
        }

        public void OnTick(float dt_) {
            try {
                CurrentState.OnTick(this, dt_, ref _isDone);
            }
            catch (Exception e) {
                Error(e);
            }
        }

        private void Transit(State target) {
            if (++transitDepth >= MaxTransitDepth) {
                Log.Assert(false, "max fsm transit depth reached. current = {0}", CurrentState.Id);
                return;
            }
            WhenTransit?.Invoke(CurrentState.Id, target.Id, transitDepth);
            try {
                var state = CurrentState;
                //check/prevent transition in state exit
                CurrentState = null;
                state.OnExit(this);
                TransitTo(target);
            }
            catch (Exception e) {
                Error(e);
            }
            --transitDepth;
        }

        private void TransitTo(State state_) {
            _isDone = state_.ShouldSkipTick;
            CurrentState = state_;
            CurrentState.OnEnter(this, _isDone);
        }

        public void SelfTransit() {
            Transit(CurrentState);
        }

        public bool DoTransition(Transition trans_) {
            if (CurrentState == null) {
                Log.Warning("transition is invalid during state exit");
                return false;
            }
            if (trans_.Source == SimpleFSMGraph.AnyState || trans_.Source == CurrentState) {
                Transit(trans_.Target);
                return true;
            }
            return false;
        }
    }

    public class SimpleFSMGraph {
        private readonly List<State> _states;
        internal State this[int index] => _states[index];
        public static State AnyState { get; private set; }
        public static State ErrorState { get; private set; }

        public SimpleFSMGraph() {
            if (AnyState == null) {
                AnyState = new State(-1, StateTransitMode.Manual);
                ErrorState = new State(-2, StateTransitMode.Manual);
            }
            _states = new List<State>(16);
        }

        public void WhenError(Action<object> Enter_, Func<object, float, bool> Tick_) {
            ErrorState.WhenEnter(Enter_);
            ErrorState.WhenTick(Tick_);
        }

        public State AddState(StateTransitMode mode_ = StateTransitMode.Automatic) {
            var state = new State(_states.Count, mode_);
            _states.Add(state);
            return state;
        }

        public Transition AddTransition(State source_, State target_, Func<object, bool, bool> Condition_ = null, int token_ = 0) {
            var trans = new Transition(source_, target_, Condition_, token_);
            source_.AddTransition(trans);
            return trans;
        }
    }

    public enum StateTransitMode {
        Automatic,
        Manual,
        Immediate,
    }

    public class State {
        public int Id { get; private set; }

        private readonly List<Transition> _transitions;
        public StateTransitMode _mode;
        private Func<object, float, bool> _OnTick;
        internal bool ShouldSkipTick => _OnTick == null;
        private Action<object> _OnTickDone;
        private Action<object> _OnEnter;
        private Action<object> _OnExit;
        private int _tokenOnTick;
        private int _tokenTickDone;
        private int _tokenOnEnter;
        private int _tokenOnExit;

        private State() {
        }

        public State(int id_, StateTransitMode mode_) {
            Id = id_;
            _transitions = new List<Transition>(2);
            _mode = mode_;
        }

        public void AddTransition(Transition trans) => _transitions.Add(trans);

        public void WhenTick(Func<object, float, bool> OnTick_, int token_ = 0) {
            _OnTick = OnTick_;
            _tokenOnTick = token_;
        }
        public void WhenTickDone(Action<object> OnTickDone_, int token_ = 0) {
            _OnTickDone = OnTickDone_;
            _tokenTickDone = token_;
        }
        public void WhenEnter(Action<object> OnEnter_, int token_ = 0) {
            _OnEnter = OnEnter_;
            _tokenOnEnter = token_;
        }
        public void WhenExit(Action<object> OnExit_, int token_ = 0) {
            _OnExit = OnExit_;
            _tokenOnExit = token_;
        }

        public void OnEnter(SimpleFSM fsm_, bool isDone_) {
            const string key = "State.OnEnter";
            Performance.RecordProfiler(key);
            _OnEnter?.Invoke(fsm_.Token[_tokenOnEnter]);
            if (_mode == StateTransitMode.Immediate) {
                CheckTransition(fsm_, isDone_);
            }
            Performance.End(key);
        }

        public void OnTick(SimpleFSM fsm_, float dt_, ref bool isDone_) {
            const string key = "State.OnTick";
            Performance.RecordProfiler(key);
            if (!isDone_) {
                isDone_ = _OnTick(fsm_.Token[_tokenOnTick], dt_);
                if (isDone_) {
                    _OnTickDone?.Invoke(fsm_.Token[_tokenTickDone]);
                }
            }
            if (_mode == StateTransitMode.Manual || fsm_.CurrentState != this) {
                //still ticking || never auto transit || external transition happend
                goto end;
            }
            CheckTransition(fsm_, isDone_);
        end:
            Performance.End(key);
        }

        private void CheckTransition(SimpleFSM fsm_, bool isDone_) {
            //give OnExit a change to execute, for dead-end states
            if (_transitions.Count == 0 && isDone_) {
                OnExit(fsm_);
                return;
            }
            foreach (var trans in _transitions) {
                if (trans.TryTransit(fsm_, isDone_)) {
                    return;
                }
            }
        }

        public void OnExit(SimpleFSM fsm_) {
            const string key = "State.OnExit";
            Performance.RecordProfiler(key);
            _OnExit?.Invoke(fsm_.Token[_tokenOnExit]);
            Performance.End(key);
        }
    }

    public class Transition {
        public State Source { get; private set; }
        public State Target { get; private set; }
        private int _token;

        private static readonly Func<object, bool, bool> _DefaultActionResultCondition = (_, c) => c;
        public static readonly Func<object, bool, bool> ConditionSkip = (_, __) => true;

        private Func<object, bool, bool> _Condition;

        private Transition() {
        }

        public Transition(State source_, State target_, Func<object, bool, bool> Condition_, int token_) {
            Source = source_;
            Target = target_;
            TransitCondition(Condition_, token_);
        }

        public void TransitCondition(Func<object, bool, bool> Condition_, int token_) {
            _token = token_;
            _Condition = Condition_ ?? _DefaultActionResultCondition;
        }

        public bool TryTransit(SimpleFSM fsm_, bool isDone_) {
            bool ret = _Condition(fsm_.Token[_token], isDone_);
            if (ret) {
                fsm_.DoTransition(this);
            }
            return ret;
        }
    }
}
