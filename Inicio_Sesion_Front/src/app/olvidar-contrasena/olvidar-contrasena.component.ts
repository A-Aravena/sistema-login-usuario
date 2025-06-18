import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ErrorPopupComponent } from '../components/error-popup/error-popup.component';
import { SuccessPopupComponent } from '../components/success-popup/success-popup.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-olvidar-contrasena',
  imports: [FormsModule, MatProgressSpinnerModule,CommonModule],
  templateUrl: './olvidar-contrasena.component.html',
  styleUrl: './olvidar-contrasena.component.css',
})
export class OlvidarContrasenaComponent {
  email: string = '';
  errorMessage: string = '';
  cargando: boolean = false;

  constructor(
    private http: HttpClient,
    private router: Router,
    private dialog: MatDialog,
    
  ) {}

  recuperarContrasena() {
    const payload = {
      email: this.email,
    };
    this.cargando = true;
    this.http
      .post<any>('https://api.bioemach.cl/recuperar/validar-email', payload)
      .subscribe({
        next: (response) => {
          this.cargando = false;
          const mensaje =
            response && response.mensaje
              ? response.mensaje
              : 'Correo enviado exitosamente. Revisa tu bandeja de entrada.';

          this.showSuccessPopup(mensaje, () => {
            this.router.navigate(['/validar-codigo'], {
              queryParams: { email: this.email },
            });
          });
        },

        error: (error) => {
          console.error('Error al enviar correo:', error);
this.cargando = false;
          const mensaje =
            error.error && error.error.mensaje
              ? error.error.mensaje
              : 'Hubo un error al enviar el correo. Por favor, intenta nuevamente.';

          this.showErrorPopup(mensaje);
        },
      });
  }

  showErrorPopup(message: string, afterCloseCallback?: () => void): void {
    const dialogRef = this.dialog.open(ErrorPopupComponent, {
      data: { message },
    });

    dialogRef.afterClosed().subscribe(() => {
      if (afterCloseCallback) {
        afterCloseCallback();
      }
    });
  }

  showSuccessPopup(message: string, afterCloseCallback?: () => void): void {
    const dialogRef = this.dialog.open(SuccessPopupComponent, {
      data: { message },
    });

    dialogRef.afterClosed().subscribe(() => {
      if (afterCloseCallback) {
        afterCloseCallback();
      }
    });
  }
}
