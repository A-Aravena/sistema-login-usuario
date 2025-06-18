import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { ErrorPopupComponent } from '../components/error-popup/error-popup.component';
import { SuccessPopupComponent } from '../components/success-popup/success-popup.component';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-restaurar-contrasena',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule],
  templateUrl: './restaurar-contrasena.component.html',
  styleUrl: './restaurar-contrasena.component.css'
})


export class RestaurarContrasenaComponent {
  form: FormGroup;
  mostrarPassword = false;
  mostrarRepetir = false;
  email = '';
  codigo = '';

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router,
    private dialog: MatDialog,
    private route: ActivatedRoute
  ) {
    this.form = this.fb.group({
      password: ['', Validators.required],
      repetir: ['', Validators.required],
    });
  }

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'];
      this.codigo = params['codigo'];
    });
  }


  restaurarContrasena() {
    const nuevaPassword = this.form.value.password;
    const repetirPassword = this.form.value.repetir;
  
    if (!nuevaPassword || !repetirPassword) {
      this.showErrorPopup('No Debe haber cambos vacios para actualizar la contraseña');
      return;
    }
  
    if (nuevaPassword !== repetirPassword) {
      this.showErrorPopup('Las contraseñas no coinciden');
      return;
    }
  
    if (!this.validarPassword(nuevaPassword)) {
      this.showErrorPopup('La contraseña debe tener\n al menos 8 dígitos, mayúsculas,\n minúsculas, un número y un carácter\n especial (# $ ! % & = ?)');
      return;
    }
  
    const url = 'https://api.bioemach.cl/recuperar/cambiar-contrasena';
    const payload = {
      email: this.email,
      codigo_validacion: this.codigo,
      nueva_contraseña: nuevaPassword
    };
  
    this.http.post<any>(url, payload).subscribe({
      next: (response) => {
        if (response?.mensaje === 'Contraseña cambiada exitosamente.') {
          this.showErrorPopup('¡Contraseña actualizada exitosamente!').subscribe(() => {
            this.router.navigate(['/']);
          });
        } else {
          this.showErrorPopup(response.mensaje || 'Error al cambiar la contraseña');
        }
      },
      error: (error) => {
        const mensaje = error.error?.mensaje || 'Error inesperado al cambiar la contraseña';
        this.showErrorPopup(mensaje);
      }
    });
  }

  showErrorPopup(message: string) {
    return this.dialog.open(ErrorPopupComponent, {
      data: { message }
    }).afterClosed();
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

  private validarPassword(password: string): boolean {
    const minLength = 8;
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumber = /[0-9]/.test(password);
    const hasSpecialChar = /[#\$!%&=\?]/.test(password);  // ojo: algunos caracteres como $ necesitan escape
    
    return (
      password.length >= minLength &&
      hasUpperCase &&
      hasLowerCase &&
      hasNumber &&
      hasSpecialChar
    );
  }

  togglePassword(campo: string): void {
    if (campo === 'nueva') {
      this.mostrarPassword = !this.mostrarPassword;
    } else if (campo === 'repetir') {
      this.mostrarRepetir = !this.mostrarRepetir;
    }
  }
  

}
