#if GENERAL_MODULE_GRPC
using Google.Protobuf;
#endif

namespace General.Module
{
    internal interface IReceiveDataModule
    {
#if GENERAL_MODULE_GRPC
        void ReceiveData<T>(T t) where T : IMessage;
#else
        void ReceiveData<T>(T t) where T : class;
#endif
    }
}


