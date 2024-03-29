namespace Oxygen
{
    public static class Response
    {
        public static Message Ack(Node node, string messageName)
        {
            return Ack(node.Name, messageName);
        }

        public static Message Ack(string nodeName, string messageName)
        {
            Message response = new Message(nodeName, messageName);
            response.WriteString("ACK");
            return response;
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
