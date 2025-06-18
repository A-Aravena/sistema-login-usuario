import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { ErrorPopupComponent } from '../components/error-popup/error-popup.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule, MatProgressSpinnerModule],
})
export class LoginComponent implements OnInit, OnDestroy {
  email: string = '';
  password: string = '';
  errorMessage: string = '';
  cargando: boolean = false;
  private popStateHandler: any;

  constructor(
    private http: HttpClient,
    private router: Router,
    private dialog: MatDialog
  ) {}

 
  ngOnInit() {
    history.pushState(null, '', window.location.href);
    this.popStateHandler = () => {
      history.pushState(null, '', window.location.href);
    };
    window.addEventListener('popstate', this.popStateHandler);
  }

  ngOnDestroy() {
    window.removeEventListener('popstate', this.popStateHandler);
  }

  login() {
    const url = 'https://api.bioemach.cl/inicio_sesion/administrador';
    const payload = {
      email: this.email,
      password: this.password,
    };

    this.cargando = true; // Mostrar el spinner

    this.http.post<any>(url, payload).subscribe({
      next: (response) => {
        if (response && response.token) {
          localStorage.setItem('token', response.token);
          this.router.navigate(['/usuario']);
        } else {
          this.showErrorPopup('Credenciales inválidas');
        }
      },
      error: (error) => {
        console.error('Error en login', error);
        this.cargando = false;
        this.showErrorPopup(
          'Credenciales inválidas\nError en correo y/o contraseña'
        );
      },
      complete: () => {
        this.cargando = false; // Ocultar el spinner
      },
    });
  }

  showErrorPopup(message: string): void {
    this.dialog.open(ErrorPopupComponent, {
      data: { message },
    });
  }

  goToForgetPassword() {
    this.router.navigate(['/olvidar-contrasena']);
  }
}
