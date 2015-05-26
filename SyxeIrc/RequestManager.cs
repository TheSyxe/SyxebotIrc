
using System;
using System.Collections.Generic;
namespace SyxeIrc
{
    public class RequestManager
    {
        public RequestManager()
        {
            PendingOperations = new Dictionary<string, RequestOperation>();
        }

        private Dictionary<string, RequestOperation> PendingOperations { get; set; }

        public void QueueOperation(string key, RequestOperation operation)
        {
            if (PendingOperations.ContainsKey(key))
                throw new InvalidOperationException("Operation is already pending.");
            PendingOperations.Add(key, operation);
        }

        public RequestOperation PeekOperation(string key)
        {
            RequestOperation operation = null;
            var realKey = PendingOperations.TryGetValue(key.ToLower(), out operation);
            //var realKey = PendingOperations.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            if (operation == null)
                throw new Exception("Could not find operation that you were looking for");
            else
                return operation;

        }

        public RequestOperation DequeueOperation(string key)
        {
            var operation = PendingOperations[key];
            PendingOperations.Remove(key);
            return operation;
        }
    }

    public class RequestOperation
    {
        public object State { get; set; }
        public Action<RequestOperation> Callback { get; set; }

        public RequestOperation(object state, Action<RequestOperation> callback)
        {
            State = state;
            Callback = callback;
        }
    }
}
