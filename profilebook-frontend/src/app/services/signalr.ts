// src/app/services/signalr.service.ts
import { Injectable, EventEmitter } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection!: signalR.HubConnection | null;
  public isConnected = false;
  private connectPromiseResolve?: () => void;
  private connectPromiseReject?: (err: any) => void;
  private connectPromise: Promise<void> | null = null;

  public onReceiveMessage = new EventEmitter<any>();
  public onMessageSent = new EventEmitter<any>();
  public onMessageRead = new EventEmitter<any>();
  public onReconnected = new EventEmitter<string>();
  public onDisconnected = new EventEmitter<void>();

  connect(token: string): Promise<void> {
    // If already connected, return resolved promise
    if (this.isConnected && this.hubConnection) {
      return Promise.resolve();
    }
    // If there is an in-flight connect, return it
    if (this.connectPromise) return this.connectPromise;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    // events
    this.hubConnection.onreconnected((id) => {
      console.log('SignalR reconnected:', id);
      this.isConnected = true;
      this.onReconnected.emit(id);
    });

    this.hubConnection.onclose((err) => {
      console.warn('SignalR connection closed', err);
      this.isConnected = false;
      this.onDisconnected.emit();
      // reset connectPromise so next connect can try again
      this.connectPromise = null;
    });

    this.hubConnection.on('ReceiveMessage', (payload: any) => this.onReceiveMessage.emit(payload));
    this.hubConnection.on('MessageSent', (payload: any) => this.onMessageSent.emit(payload));
    this.hubConnection.on('MessageRead', (payload: any) => this.onMessageRead.emit(payload));

    this.connectPromise = new Promise<void>((resolve, reject) => {
      this.connectPromiseResolve = resolve;
      this.connectPromiseReject = reject;

      this.hubConnection!.start()
        .then(() => {
          console.log('SignalR connected');
          this.isConnected = true;
          resolve();
        })
        .catch(err => {
          console.error('SignalR failed to connect:', err);
          this.isConnected = false;
          this.connectPromise = null;
          reject(err);
        });
    });

    return this.connectPromise;
  }

  // helper for components that need to wait
  waitForConnection(timeoutMs = 10000): Promise<void> {
    if (this.isConnected) return Promise.resolve();
    if (this.connectPromise) {
      // return the in-flight connect promise but with timeout
      return new Promise((resolve, reject) => {
        const to = setTimeout(() => reject(new Error('SignalR connection timeout')), timeoutMs);
        this.connectPromise!.then(() => { clearTimeout(to); resolve(); }).catch(err => { clearTimeout(to); reject(err); });
      });
    }
    return Promise.reject(new Error('SignalR not started'));
  }

  sendMessage(receiverId: number, text: string): Promise<void> {
    if (!this.hubConnection || !this.isConnected) {
      return Promise.reject(new Error('Not connected'));
    }
    return this.hubConnection.invoke('SendMessage', receiverId, text);
  }

  markRead(messageId: number): Promise<void> {
    if (!this.hubConnection || !this.isConnected) {
      return Promise.reject(new Error('Not connected'));
    }
    return this.hubConnection.invoke('MarkRead', messageId);
  }

  disconnect(): Promise<void> | void {
    if (!this.hubConnection) return;
    return this.hubConnection.stop().then(() => {
      this.isConnected = false;
      this.connectPromise = null;
      this.hubConnection = null;
    });
  }
}
