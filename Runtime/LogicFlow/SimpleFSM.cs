using System;
using Transient.SimpleContainer;

namespace Transient.ControlFlow {
    public class FSMStack {
        private readonly List<(FSMGraph, State, object[])> _stacked;
        private readonly SimpleFSM _active;

        public FSMStack(SimpleFSM fsm_) {
            _active = fsm_;
            _stacked = new List<(FSMGraph, State, object[])>(4);
        }

        public void Push(FSMGraph graph_, State entry_, State exit_, object[] token_, bool skip_ = false) {
            #if DEBUG
            if (_active.Graph == graph_) {
                throw new Exception("pushing duplicate fsm graph");
            }
            #endif
            if (exit_ == null || !_active.Graph.Contains(exit_)) {
                exit_ = _active.CurrentState;
            }
            _stacked.Push((_active.Graph, exit_, _active.Token));
            _active.Init(graph_, token_);
            if (!skip_) {
                _active.Reset(entry_);
            }
        }

        public void Pop() {
            var (graph, state, token) = _stacked.Pop();
            _active.Init(graph, token);
            _active.Reset(state);
        }

        public void Clear() {
            if (_stacked.Count == 0) return;
            var (graph, _, token) = _stacked[0];
            _active.Init(graph, token);
            _stacked.Clear();
        }
    }

    public class SimpleFSM {
        public FSMGraph Graph { get; set; }
        public State CurrentState { get; private set; }
        private State ActingState;
        public object[] Token { get; private set; }
        private bool _isDone;
        public State ErrorState { get; private set; }
        public Action<int, int, int> WhenTransit { get; set; }
#if FSMTimeoutCheck
        private Action<int> WhenTimeout { get; set; }
        private float timeout;
        private float lastTransitTime;
#endif
        private int transitDepth = 0;
        public static int MaxTransitDepth = 16;
        private static readonly object[] DefaultToken = new object[1];
        private Dictionary<State, State> StateOverrideRule { get; set; }

        public bool IsInState(int id_) => CurrentState.Id == id_;

        public SimpleFSM() {
            ErrorState = new State(-200, StateTransitMode.Manual);
        }

        public void Init(FSMGraph graph_, params object[] token_) {
            Log.Assert(graph_ != null, "invalid fsm graph");
            transitDepth = 0;
            Graph = graph_;
            Token = token_ ?? Token ?? DefaultToken;
        }

        public void Reset(State state_ = null) {
            try {
                state_ = state_ ?? Graph[0];
                WhenTransit?.Invoke(-1, state_.Id, transitDepth);
                TransitTo(state_);
            }
            catch (Exception e) {
                Error(e);
            }
        }

        private void Error(Exception e_) {
            Log.Error($"{e_.Message}\n{e_.StackTrace}");
            TransitTo(ErrorState);
        }

#if FSMTimeoutCheck
        public void TimeoutCheck(float timeout_, Action<int> WhenTimeout_) {
            timeout = timeout_;
            WhenTimeout = WhenTimeout_;
        }
#endif

        public void OverrideState(State origin, State target) {
            StateOverrideRule = StateOverrideRule ?? new Dictionary<State, State>(4);
            StateOverrideRule[origin] = target;
        }

        public void OnTick(float dt_) {
            try {
                ActingState.OnTick(this, dt_, ref _isDone, CurrentState);
            }
            catch (Exception e) {
                Error(e);
            }
#if FSMTimeoutCheck
            if (timeout > 0 && UnityEngine.Time.time - lastTransitTime > timeout) {
                WhenTimeout(CurrentState.Id);
            }
#endif
        }

        private void Transit(State target_) {
            if (++transitDepth >= MaxTransitDepth) {
                Error(new Exception($"max fsm transit depth reached. current state = {CurrentState.Id}"));
                return;
            }
            WhenTransit?.Invoke(CurrentState.Id, target_.Id, transitDepth);
            try {
                //check/prevent transition in state exit
                CurrentState = null;
                ActingState.OnExit(this);
                TransitTo(target_);
            }
            catch (Exception e) {
                Error(e);
            }
            --transitDepth;
        }

        private void TransitTo(State state_) {
            CurrentState = state_;
            ActingState = state_;
            StateOverrideRule?.TryGetValue(state_, out ActingState);
            ActingState = ActingState ?? state_;
            _isDone = ActingState.ShouldSkipTick;
            ActingState.OnEnter(this, _isDone, CurrentState);
            ActingState.ExitIfDeadEnd(this, _isDone, CurrentState);
#if FSMTimeoutCheck
            lastTransitTime = UnityEngine.Time.time;
#endif
        }

        public void SelfTransit() {
            Transit(CurrentState);
        }

        public bool DoTransition(Transition trans_) {
            if (CurrentState == null) {
                Log.Warning("transition is invalid during state exit");
                return false;
            }
            if (trans_.Source == FSMGraph.AnyState || trans_.Source == CurrentState) {
                Transit(trans_.Target);
                return true;
            }
            return false;
        }
    }

    public class FSMGraph {
        private readonly List<State> _states;
        internal State this[int index] => _states[index];
        public static State AnyState { get; private set; }

        public FSMGraph() {
            if (AnyState == null) {
                AnyState = new State(-1, StateTransitMode.Manual);
            }
            _states = new List<State>(16);
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

        public bool Contains(State state_) {
            return _states.Contains(state_);
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
        private int _tokenOnTickDone;
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
#if ProhibitEmptyFSMCallbackAssign
            Log.Assert(OnTick_ != null, "empty on tick callback, {0}", Id);
#endif
            _OnTick = OnTick_;
            _tokenOnTick = token_;
        }
        public void WhenTickDone(Action<object> OnTickDone_, int token_ = 0) {
#if ProhibitEmptyFSMCallbackAssign
            Log.Assert(OnTickDone_ != null, "empty on tick done callback, {0}", Id);
#endif
            _OnTickDone = OnTickDone_;
            _tokenOnTickDone = token_;
        }
        public void WhenEnter(Action<object> OnEnter_, int token_ = 0) {
#if ProhibitEmptyFSMCallbackAssign
            Log.Assert(OnEnter_ != null, "empty on enter callback, {0}", Id);
#endif
            _OnEnter = OnEnter_;
            _tokenOnEnter = token_;
        }
        public void WhenExit(Action<object> OnExit_, int token_ = 0) {
#if ProhibitEmptyFSMCallbackAssign
            Log.Assert(OnExit_ != null, "empty on exit callback, {0}", Id);
#endif
            _OnExit = OnExit_;
            _tokenOnExit = token_;
        }

        internal void OnEnter(SimpleFSM fsm_, bool isDone_, State TransitionTarget) {
            const string key = "State.OnEnter";
            Performance.RecordProfiler(key);
            _OnEnter?.Invoke(fsm_.Token[_tokenOnEnter]);
            if (TransitionTarget._mode == StateTransitMode.Immediate) {
                TransitionTarget.CheckTransition(fsm_, isDone_);
            }
            Performance.End(key);
        }

        internal void OnTick(SimpleFSM fsm_, float dt_, ref bool isDone_, State TransitionTarget) {
            const string key = "State.OnTick";
            Performance.RecordProfiler(key);
            if (!isDone_) {
                Log.Assert(_OnTick != null, "{0} invalid OnTick", this);
                isDone_ = _OnTick(fsm_.Token[_tokenOnTick], dt_);
                if (isDone_) {
                    _OnTickDone?.Invoke(fsm_.Token[_tokenOnTickDone]);
                    ExitIfDeadEnd(fsm_, true, TransitionTarget);
                }
                else {
                    goto end;
                }
            }
            if (TransitionTarget._mode == StateTransitMode.Manual || fsm_.CurrentState != TransitionTarget) {
                //still ticking || never auto transit || external transition happend
                goto end;
            }
            TransitionTarget.CheckTransition(fsm_, isDone_);
        end:
            Performance.End(key);
        }

        private void CheckTransition(SimpleFSM fsm_, bool isDone_) {
            foreach (var trans in _transitions) {
                if (trans.TryTransit(fsm_, isDone_)) {
                    return;
                }
            }
        }

        //give OnExit a change to execute, for dead-end states
        internal void ExitIfDeadEnd(SimpleFSM fsm_, bool isDone_, State TransitionTarget) {
            if (TransitionTarget._transitions.Count == 0
             && TransitionTarget._mode != StateTransitMode.Manual
             && fsm_.CurrentState == TransitionTarget
             && isDone_) {
                OnExit(fsm_);
            }
        }

        internal void OnExit(SimpleFSM fsm_) {
            const string key = "State.OnExit";
            Performance.RecordProfiler(key);
            _OnExit?.Invoke(fsm_.Token[_tokenOnExit]);
            Performance.End(key);
        }

        public override string ToString() {
            return $"State {Id}";
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
