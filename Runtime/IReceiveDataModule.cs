#if grpc
using Google.Protobuf;
#endif

namespace General.Module
{
    internal interface IReceiveDataModule
    {
#if grpc
        void ReceiveData<T>(T t) where T : IMessage;
#else
        void ReceiveData<T>(T t) where T : class;
#endif
    }
}


