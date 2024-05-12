public interface IMessageChecker
{
    void CheckImportantMessageConfirmation((MessageType, ulong) data);
}