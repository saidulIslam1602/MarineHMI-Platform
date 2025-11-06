using KChief.Platform.Core.Interfaces;
using Opc.Ua;
using Opc.Ua.Client;

namespace KChief.Protocols.OPC.Services;

/// <summary>
/// OPC UA client service implementation.
/// This is a simplified implementation for MVP purposes.
/// </summary>
public class OPCUaClientService : IOPCUaClient, IDisposable
{
    private Session? _session;
    private SessionReconnectHandler? _reconnectHandler;
    private bool _disposed = false;

    public bool IsConnected => _session?.Connected ?? false;

    public async Task<bool> ConnectAsync(string endpointUrl)
    {
        try
        {
            var applicationConfiguration = new ApplicationConfiguration
            {
                ApplicationName = "K-Chief OPC UA Client",
                ApplicationUri = Utils.Format(@"urn:{0}:KChiefOPCClient", System.Net.Dns.GetHostName()),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/DefaultApplications",
                        SubjectName = "K-Chief OPC UA Client"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/UA Certificate Authorities"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/UA Applications"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/RejectedCertificates"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };

            await applicationConfiguration.Validate(ApplicationType.Client);

            var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointUrl, useSecurity: false);
            var endpointConfiguration = EndpointConfiguration.Create(applicationConfiguration);
            var configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            var userIdentity = new UserIdentity(new AnonymousIdentityToken());
            _session = await Session.Create(
                applicationConfiguration,
                configuredEndpoint,
                false,
                "K-Chief OPC UA Client",
                60000,
                userIdentity,
                null);

            if (_session != null && _session.Connected)
            {
                _reconnectHandler = new SessionReconnectHandler();
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_session != null)
        {
            _reconnectHandler?.Dispose();
            await _session.CloseAsync();
            _session.Dispose();
            _session = null;
        }
    }

    public async Task<object?> ReadNodeValueAsync(string nodeId)
    {
        if (_session == null || !_session.Connected)
        {
            throw new InvalidOperationException("OPC UA client is not connected.");
        }

        try
        {
            var readValueId = new ReadValueId
            {
                NodeId = new NodeId(nodeId),
                AttributeId = Attributes.Value
            };

            var readValueIdCollection = new ReadValueIdCollection { readValueId };
            var response = await _session.ReadAsync(null, 0, TimestampsToReturn.Neither, readValueIdCollection, CancellationToken.None);

            if (response.Results.Count > 0 && StatusCode.IsGood(response.Results[0].StatusCode))
            {
                return response.Results[0].Value;
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> WriteNodeValueAsync(string nodeId, object value)
    {
        if (_session == null || !_session.Connected)
        {
            throw new InvalidOperationException("OPC UA client is not connected.");
        }

        try
        {
            var writeValue = new WriteValue
            {
                NodeId = new NodeId(nodeId),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };

            var writeValueCollection = new WriteValueCollection { writeValue };
            var response = await _session.WriteAsync(null, writeValueCollection, CancellationToken.None);

            return response.Results.Count > 0 && StatusCode.IsGood(response.Results[0]);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Task<bool> SubscribeToNodeAsync(string nodeId, Action<string, object?> onValueChanged)
    {
        // Simplified subscription - full implementation would require subscription management
        // This is a placeholder for MVP
        return Task.FromResult(false);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _disposed = true;
        }
    }
}

