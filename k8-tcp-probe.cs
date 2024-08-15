namespace K8
{
  using System;
  using System.Net;
  using System.Net.Sockets;
  using Microsoft.Extensions.Logging;

  public class TcpProbe : IDisposable
  {
    private readonly AsyncCallback callback;
    private readonly object syncObject = new object();
    private readonly Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly ILogger<TcpProbe>? logger;
    private bool disposedValue = false;
    private Socket? connectedSocket;

    public TcpProbe(){
      this.callback = new AsyncCallback(this.OnAccept);
    }

    public TcpProbe(ILogger<TcpProbe> logger)
      : this()
    {
      this.logger = logger;
    }

    public void StartProbeAsync(int port = 25000){
      this.ThrowIfDisposed();
      this.listener.Bind(new IPEndPoint(IPAddress.Any, port));
      this.listener.Listen(4);
      this.listener.BeginAccept(this.callback, null);
    }    

    public void StopProbeAsync(){
      this.ThrowIfDisposed();
      this.listener?.Dispose();      
    }

    public void Dispose(){
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing){
     if(!this.disposedValue){
      lock(this.syncObject){
        if(!this.disposedValue){
          if(disposing){
            this.SafeDisposeOfConnectedSocket();
            this.SafeDisposeOfListener();
          }
          this.disposedValue = true;
        }
      }
     }else{
      this.logger?.LogInformation(Resources.ListenerDisposed);
     }
    }
    
    private void SafeDisposeOfListener(){
      try{
        this.listener?.Dispose();
      }
      catch(ObjectDisposedException){
        // Do nothing, already called.    
      }
    }

    private void ThrowIfDisposed(){
      if(this.disposedValue){
        throw new ObjectDisposedException(this.GetType().Name);
      }
    }

    private void SafeDisposeOfConnectedSocket(){
      try{
        this.connectedSocket?.Dispose();
      }
      catch(ObjectDisposedException){
        // Do nothing, already called.
      }
    }

    private void OnAccept(IAsyncResult result){
      this.SafeDisposeOfConnectedSocket();
   
      try{
        if(!this.disposedValue){
          lock(this.syncObject){
            if(!this.disposedValue){
              this.logger?.LogInformation(Resources.AcceptingConnection);
              try{
                this.connectedSocket = this.listener?.EndAccept(result);
                this.logger?.LogInformation(Resources.AcceptedConnection);
              }
              catch(ObjectDisposedException){
                // Do nothing, already called.
              }
            }
            else{
              this.logger?.LogInformation(Resources.ListenerDisposed);
            }
          }
        }else{
          this.logger?.LogInformation(Resources.ListenerDisposed);
        }
      }
      finally{
        if(!this.disposedValue){
          lock(this.syncObject){
            if(!this.disposedValue){
              this.logger?.LogInformation(Resources.BeginAcceptingConnection);

              try{
                this.listener?.BeginAccept(this.callback, null);
              }
              catch(ObjectDisposedException){
                // Do nothing, already called.
              }
            }else{
              this.logger?.LogInformation(Resources.ListenerDisposed);
            }
          }
        }
      }
    }

    private static class Resources
    {
      public static readonly string ListenerDisposed = "Listener Disposed";
      public static readonly string BeginAcceptingConnection = "Begin Accepting Connection";
      public static readonly string AcceptingConnection = "Accepting Connection";      
      public static readonly string AcceptedConnection = "Accepted Connection";      
    }
  }
}
