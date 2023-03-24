using System;
using System.Collections.Generic;
using Transient.Container;

namespace Transient {
    internal struct ActionWithRef<A> {
        public A Value { get; set; }
        public object token;
    }

    public abstract class ActionListAbstract<AT> {
        private protected List<ActionWithRef<AT>> _list;
        private readonly string _name;

        public ActionListAbstract(int capacity, string name = null) {
            _name = name ?? GetHashCode().ToString();
            _list = new List<ActionWithRef<AT>>(capacity);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void CheckDuplicate(AT value_) {
            foreach (var v in _list) {
                if (ReferenceEquals(v.Value, value_)) {
                    Log.Warn($"trying to add duplicate action to list({_name})!");
                    return;
                }
            }
        }

        //no effort is made to prevent duplicate add
        public void Add(AT add_, object token_) {
            CheckDuplicate(add_);
            _list.Add(new ActionWithRef<AT>() { Value = add_, token = token_ });
            //Log.Debug($"Register {_name} from {(add as Delegate).Method.DeclaringType.Name}");
        }
        public void Add((AT, object) add_) {
            CheckDuplicate(add_.Item1);
            _list.Add(new ActionWithRef<AT>() { Value = add_.Item1, token = add_.Item2 });
            //Log.Debug($"Register {_name} from {(add as Delegate).Method.DeclaringType.Name}");
        }

        public void Remove(object token_) {
            for (int r = 0; r < _list.Count; ++r) {
                if (_list[r].token == token_) {
                    _list.OutOfOrderRemoveAt(r);
                    //Log.Debug($"Unregister {_name} from {(remove as Delegate).Method.DeclaringType.Name}");
                }
            }
        }
        public override string ToString() {
            return $"{_name}(count={_list.Count})";
        }
        public void Clear() {
            _list.Clear();
        }
    }

    public sealed class ActionList : ActionListAbstract<Action> {
        public ActionList(int capacity, string name = null) : base(capacity, name) {

        }
        public static ActionList operator +(ActionList self, (Action, object) add) {
            self.Add(add);
            return self;
        }
        public static ActionList operator -(ActionList self, Action remove) {
            self.Remove(remove);
            return self;
        }
        public void Invoke() {
            foreach(var A in _list)
                A.Value();
        }
    }

    public sealed class ActionList<T> : ActionListAbstract<Action<T>> {
        public ActionList(int capacity, string name = null) : base(capacity, name) {

        }
        public static ActionList<T> operator +(ActionList<T> self, (Action<T>, object) add) {
            self.Add(add);
            return self;
        }
        public static ActionList<T> operator -(ActionList<T> self, object remove) {
            self.Remove(remove);
            return self;
        }
        public void Invoke(T t) {
            foreach(var A in _list)
                A.Value(t);
        }
    }

    public sealed class ActionList<T1, T2> : ActionListAbstract<Action<T1, T2>> {
        public ActionList(int capacity, string name = null) : base(capacity, name) {

        }
        public static ActionList<T1, T2> operator +(ActionList<T1, T2> self, (Action<T1, T2>, object) add) {
            self.Add(add);
            return self;
        }
        public static ActionList<T1, T2> operator -(ActionList<T1, T2> self, object remove) {
            self.Remove(remove);
            return self;
        }
        public void Invoke(T1 t1, T2 t2) {
            foreach(var A in _list)
                A.Value(t1, t2);
        }
    }

    public sealed class ActionList<T1, T2, T3> : ActionListAbstract<Action<T1, T2, T3>> {
        public ActionList(int capacity, string name = null) : base(capacity, name) {

        }
        public static ActionList<T1, T2, T3> operator +(ActionList<T1, T2, T3> self, (Action<T1, T2, T3>, object) add) {
            self.Add(add);
            return self;
        }
        public static ActionList<T1, T2, T3> operator -(ActionList<T1, T2, T3> self, object remove) {
            self.Remove(remove);
            return self;
        }
        public void Invoke(T1 t1, T2 t2, T3 t3) {
            foreach(var A in _list)
                A.Value(t1, t2, t3);
        }
    }
}

namespace Transient {
    public struct BindingAction<T, R> {
        public Action<T, object> Value { get; set; }
        public R target;
    }

    public sealed class ActionListBinding<T, R> : ActionListAbstract<BindingAction<T, R>> {
        public ActionListBinding(int capacity, string name = null) : base(capacity, name) {

        }
        public void Add(Action<T, object> add_, R target_, object token_ = null) {
            if (target_ == null) {
                Log.Warn("invalid binding target");
                return;
            }
            Add(new BindingAction<T, R>() { Value = add_, target = target_ }, token_ ?? target_);
        }
        public void Invoke(T t) {
            foreach (var A in _list)
                A.Value.Value(t, A.Value.target);
        }
    }

    public sealed class ActionListBindingFlex<T, R> : ActionListAbstract<BindingAction<T, object>> {
        public ActionListBindingFlex(int capacity, string name = null) : base(capacity, name) {

        }
        public void Add(Action<T, object> add_, R target_, object token_ = null) {
            if (target_ == null) {
                Log.Warn("invalid binding target");
                return;
            }
            Add(new BindingAction<T, object>() { Value = add_, target = target_ }, token_ ?? target_);
        }
        public void Invoke(T t) {
            foreach (var A in _list)
                A.Value.Value(t, (R)A.Value.target);
        }
    }
}