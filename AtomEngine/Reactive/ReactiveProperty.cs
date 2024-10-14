using System.Reactive.Subjects;
using System.Reactive.Linq;
using System;

namespace AtomEngine.Reactive
{
    public class ReactiveProperty<T> : IDisposable
    {
        private BehaviorSubject<T> _subject;
        private T _value;

        public ReactiveProperty(T initialValue = default)
        {
            _value = initialValue;
            _subject = new BehaviorSubject<T>(_value); 
        }

        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    _subject.OnNext(_value);
                }
            }
        }

        public IObservable<T> AsObservable() => _subject.AsObservable();
        public IDisposable Subscribe(IObserver<T> observer) => _subject.Subscribe(observer);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _subject?.Dispose();
            }
        }
    }
}
