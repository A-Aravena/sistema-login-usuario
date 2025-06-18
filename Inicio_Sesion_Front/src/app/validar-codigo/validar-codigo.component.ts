// validar-codigo.component.ts
import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ErrorPopupComponent } from '../components/error-popup/error-popup.component';
import { SuccessPopupComponent } from '../components/success-popup/success-popup.component';
import { MatDialog } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-validar-codigo',
  templateUrl: './validar-codigo.component.html',
  styleUrl: './validar-codigo.component.css',
  imports: [FormsModule]
})
export class ValidarCodigoComponent implements OnInit {
  email: string = '';
  codigo: string = '';
  errorMessage: string = '';

  constructor(
    private http: HttpClient, 
    private router: Router, 
    private route: ActivatedRoute,
    private dialog: MatDialog
  ) {}

  ngOnInit() {

    this.route.queryParams.subscribe(params => {
      this.email = params['email'];
    });
  }

  validarCodigo() {
    const body = { email: this.email, codigo_validacion: this.codigo };

    this.http.post('https://api.bioemach.cl/recuperar/validar-codigo', body, {
      headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' }
    }).subscribe({
      next: (response) => {
        console.log('Código validado correctamente', response);
        this.errorMessage = '';
      
        this.showSuccessPopup('¡Código validado correctamente!', () => {
          this.router.navigate(['/restaurar-contrasena'], {
            queryParams: { email: this.email, codigo: this.codigo }
          });
        });
      },
      error: (error) => {
        console.error('Error al validar código:', error);

        const mensaje = (error.error && error.error.mensaje)
          ? error.error.mensaje
          : 'Hubo un error al validar el código. Por favor, intenta nuevamente.';

        // Si el mensaje es el específico, agregamos la redirección
        if (mensaje.includes('Código inválido')) {
          this.showErrorPopup(mensaje, () => {
            this.router.navigate(['/olvidar-contrasena']);
          });
        } else {
          this.showErrorPopup(mensaje);
        }
      }
    });
  }

  showErrorPopup(message: string, afterCloseCallback?: () => void): void {
    const dialogRef = this.dialog.open(ErrorPopupComponent, {
      data: { message }
    });

   
    dialogRef.afterClosed().subscribe(() => {
      if (afterCloseCallback) {
        afterCloseCallback();
      }
    });
  }



  showSuccessPopup(message: string, afterCloseCallback?: () => void): void {
    const dialogRef = this.dialog.open(SuccessPopupComponent, {
      data: { message }
    });
  
    dialogRef.afterClosed().subscribe(() => {
      if (afterCloseCallback) {
        afterCloseCallback();
      }
    });
  }
}