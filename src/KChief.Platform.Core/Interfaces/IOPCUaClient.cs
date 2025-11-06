namespace KChief.Platform.Core.Interfaces;

/// <summary>
/// Interface for OPC UA client operations.
/// </summary>
public interface IOPCUaClient
{
    /// <summary>
    /// Connects to an OPC UA server.
    /// </summary>
    Task<bool> ConnectAsync(string endpointUrl);

    /// <summary>
    /// Disconnects from the OPC UA server.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Checks if the client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Reads a node value from the OPC UA server.
    /// </summary>
    Task<object?> ReadNodeValueAsync(string nodeId);

    /// <summary>
    /// Writes a value to an OPC UA node.
    /// </summary>
    Task<bool> WriteNodeValueAsync(string nodeId, object value);

    /// <summary>
    /// Subscribes to node value changes.
    /// </summary>
    Task<bool> SubscribeToNodeAsync(string nodeId, Action<string, object?> onValueChanged);
}

