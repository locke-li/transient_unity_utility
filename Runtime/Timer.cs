//
// timer execution
//
// author: liyingbo
//
using Transient.SimpleContainer;

namespace Transient {
    public class Timer {
        private struct Interval {
            public float time;
            public float count;

            public bool Step(ref float time_, ref int count_, ref int current_) {
                if(time > time_)
                    return false;
                time_ = 0;
                if(count > ++count_)
                    return true;
                count_ = 0;
                ++current_;
                return true;
            }
        }

        #region type control

        private static List<Timer> _timers = new List<Timer>(32);
        private static List<Timer> _reusable = new List<Timer>(32, 4);

        private static Timer Use(ActionList<float> invoker = null) {
            Timer ret = _reusable.Count > 0 ? _reusable.Pop() : new Timer();
            ret.RegisterTo(invoker);
            return ret;
        }

        public static void Truncate() {
            _timers.RemoveAll(timer => !timer.Active);
            _reusable.Clear();
        }

        public static void Clear() {
            foreach(Timer timer in _timers) {
                timer.Cancel();
            }
            _reusable.Clear();
        }

        #endregion type control

        public bool Active { get; private set; }
        private ActionList<float> _invoker = null;
        private Action OnTrigger;
        private Interval[] mIntervals;
        private int mInterCount;
        private int mCurrent;
        private int mCount;
        private float mTime;

        private Timer() {
            mIntervals = new Interval[8];
            _timers.Add(this);
        }

        public static Timer Execute(Action TimerAction) {
            if(TimerAction == null) {
                Log.Warning("executing null action with Timer!");
                return null;
            }
            Timer ret = Use();
            ret.OnTrigger = TimerAction;
            ret.Active = true;
            ret.mInterCount = -1;
            ret.mCurrent = 0;
            return ret;
            //TODO the returned timer may become recycled and reused,
            //but the previous external timer owner won't realize it was reused,
            //and when it is Canceled by the previous owner, the new timer action is Canceled 
        }

        public Timer WithDelay(float delay_) {
            return WithInterval(delay_, 1);
        }

        public Timer WithInterval(float interval_, int count_ = 0) {
            mIntervals[++mInterCount] = new Interval {
                time = interval_,
                count = count_
            };
            return this;
        }

        private void RegisterTo(ActionList<float> invoker) {
            _invoker = invoker;
            if(_invoker == null)
                return;
            _invoker += (Step, this);
        }

        private void Step(float deltaTime_) {
            mTime += deltaTime_;
#if DEBUG
            if(mCurrent < 0 || mCurrent >= mIntervals.Length) {
                Cancel();
            }
#endif
            if(mIntervals[mCurrent].Step(ref mTime, ref mCount, ref mCurrent)) {
                OnTrigger();
                if(mCurrent > mInterCount) {
                    Cancel();
                }
            }
        }

        public void Invoke() {
            if(!Active)
                return;
            OnTrigger();
            Active = false;
            _reusable.Push(this);
            if(_invoker != null) {
                _invoker -= this;
                _invoker = null;
            }
        }

        public void Cancel() {
            if(!Active)
                return;
            OnTrigger = null;
            Active = false;
            _reusable.Push(this);
            if(_invoker != null) {
                _invoker -= this;
                _invoker = null;
            }
        }
    }
}
