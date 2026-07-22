import { Router } from '@angular/router';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Auth, LoginResponse } from '../../core/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule, CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  username = '';
  password = '';
  result: LoginResponse | null = null;
  error = '';
  loading = false;
  challengeMode = false;
  otpInput = '';
  otpHint = '';        // OTP yang ditampilkan hanya untuk versi demo
  otpError = '';
  blockedMinutes: number | null = null;

  constructor(private auth: Auth, private router: Router) {}

  onSubmit() {
    this.loading = true;
    this.result = null;
    this.error = '';
    this.challengeMode = false;
    this.blockedMinutes = null;

    this.auth.login({ username: this.username, password: this.password })
      .subscribe({
        next: (res) => {
          this.result = res;
          this.loading = false;

          if (res.token) {
          this.auth.saveToken(res.token);
          this.router.navigate(['/home']);
        }
        },
        error: (err) => {
          this.loading = false;
          const body = err.error ?? {};
          this.result = body;

          if (body.decision === 'CHALLENGE') {
            this.challengeMode = true;
            this.otpHint = body.otp ?? '';
          } else if (body.locked || body.decision === 'BLOCK') {
            this.blockedMinutes = body.remainingMinutes ?? null;
            this.error = body.message ?? 'Akses diblokir.';
          } else {
            this.error = body.message ?? 'Login gagal.';
          }
        }
      }
    );
  }

  onVerifyOtp() {
    this.otpError = '';
    this.loading = true;
    this.auth.verifyOtp(this.username, this.otpInput).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.token) {
          this.auth.saveToken(res.token);
          this.router.navigate(['/home']);
        }
      },
      error: (err) => {
        this.loading = false;
        this.otpError = err.error?.message ?? 'Verifikasi gagal.';
      }
    });
  }
}
