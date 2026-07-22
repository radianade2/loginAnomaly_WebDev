import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Auth } from '../../core/auth';

@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  constructor(private auth: Auth, private router: Router) {}

  onLogout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
