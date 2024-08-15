namespace K8
{
  using System;
  using System.Net;
  using System.Net.Sockets;
  using Microsoft.Extensions.Logging;

  public class TcpProbe : IDisposable
  {
    private readonly AsyncCallback callback;
    private readonly object syncObject = new ();
    private readonly Socket listener = new (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
      this.StartProbeAsync(new IPEndPoint(IPAddress.Any, port));
    }    

    public void StopProbeAsync(){
      if(this.disposedValue){
        throw new ObjectDisposedException(this.GetType().Name);
      }

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
            try{
              this.connectedSocket?.Dispose();
            }
            catch(ObjectDisposedException){
              // Do nothing, already called.
            }

            try{
             this.listener?.Dispose();
            }
            catch(ObjectDisposedException){
              // Do nothing, already called.    
            }
          }
          this.disposedValue = true;
        }
      }
     } 
    }

    private void StartProbeAsync(EndPoint endPoint){
      if(this.disposedValue){
        throw new ObjectDisposedException(this.GetType().Name);
      }

      this.listener.Bind(endPoint);
      this.listener.Listen(4);
      this.listener.BeginAccept(this.callback, null);
    }

    private void OnAccept(IAsyncResult result){
      try{
        this.connectedSocket?.Dispose();
      }
      catch(ObjectDisposedException){
        // Do nothing, already called.
      }
      
      try{
        if(!this.disposedValue){
          lock(this.syncObject){
            if(!this.disposedValue){
              this.logger?.LogInformation(Resources.AcceptingConnection);

              try{
                this.connectedSocket = this.listener?.EndAccept(result);
              }
              catch(ObjectDisposedException){
                // Do nothing, already called.
              }

              this.logger?.LogInformation(Resources.AcceptingConnection);
            }
            else{
              this.logger?.LogInformation(Resources.ListenerDisposed);
            }
          }
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
    }
  }
}
