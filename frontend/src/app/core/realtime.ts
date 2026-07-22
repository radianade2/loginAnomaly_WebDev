import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { LoginEventDto, AlertDto } from './monitoring';

@Injectable({ providedIn: 'root' })
export class Realtime {
  private hubUrl = 'http://localhost:5108/hubs/monitoring';  // sesuaikan port
  private connection?: signalR.HubConnection;

  // "Subject" = saluran yang bisa dipancarkan & didengar komponen
  loginEvent$ = new Subject<LoginEventDto>();
  alert$ = new Subject<AlertDto>();

  start(): void {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect()
      .build();

    // Dengarkan saluran "loginEvent" dari backend
    this.connection.on('loginEvent', (data: LoginEventDto) => {
      this.loginEvent$.next(data);
    });

    // Dengarkan saluran "alert" dari backend
    this.connection.on('alert', (data: AlertDto) => {
      this.alert$.next(data);
    });

    this.connection.start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error('SignalR error:', err));
  }

  stop(): void {
    this.connection?.stop();
  }
}
