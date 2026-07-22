import { Component, OnInit, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Monitoring, LoginEventDto, AlertDto, SummaryDto } from '../../core/monitoring';
import { Realtime } from '../../core/realtime';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy  {
  events: LoginEventDto[] = [];
  alerts: AlertDto[] = [];
  summary: SummaryDto = { total: 0, suspicious: 0, activeAlerts: 0 };

  constructor(private monitoring: Monitoring, private realtime: Realtime, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadAll();
    this.realtime.start();

    this.realtime.loginEvent$.subscribe(ev => {
      this.events.unshift(ev);
      this.summary.total++;
      if (ev.riskScore >= 25) this.summary.suspicious++;
      this.cdr.detectChanges();
    });

    // Alert baru masuk -> taruh di atas + tambah counter
    this.realtime.alert$.subscribe(al => {
      this.alerts.unshift(al);
      this.summary.activeAlerts++;
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy() {
    this.realtime.stop();
  }

  loadAll() {
    this.monitoring.getSummary().subscribe(s => {this.summary = s; this.cdr.detectChanges();});
    this.monitoring.getEvents(50).subscribe(e => {this.events = e; this.cdr.detectChanges();});
    this.monitoring.getAlerts(false).subscribe(a => {this.alerts = a; this.cdr.detectChanges();});
  }

  acknowledge(id: number) {
    this.monitoring.acknowledge(id).subscribe(() => this.loadAll());
  }
}
