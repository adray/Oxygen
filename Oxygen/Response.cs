namespace Oxygen
{
    internal static class Response
    {
        public static Message Ack(Node node, string messageName)
        {
            return new Message(node.Name, messageName);
        }

        public static Message Ack(string nodeName, string messageName)
        {
            return new Message(nodeName, messageName);
        }

        public static Message Nack(Node node, int errorCode, string errorMsg, string messageName)
        {
            return Nack(node.Name, errorCode, errorMsg, messageName);
        }

        public static Message Nack(string nodeName, int errorCode, string errorMsg, string messageName)
        {
            Message response = new Message(nodeName, messageName);
            response.WriteString("NACK");
            response.WriteInt(errorCode);
            response.WriteString(errorMsg);
            return response;
        }
    }
}
