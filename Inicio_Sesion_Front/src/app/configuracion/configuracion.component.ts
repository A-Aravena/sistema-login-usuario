import { Component, OnInit } from '@angular/core';
import { MenuLateralComponent } from '../components/menu-lateral/menu-lateral.component';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { NgxPaginationModule } from 'ngx-pagination';
import { FormsModule } from '@angular/forms'; // Importar FormsModule para usar ngModel
import { MatIconModule } from '@angular/material/icon';
import { ErrorPopupComponent } from '../components/error-popup/error-popup.component';
import { SuccessPopupComponent } from '../components/success-popup/success-popup.component';
import { InformationPopupComponent } from '../components/information-popup/information-popup.component';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { getSpanishPaginatorIntl } from '../utils/paginator-intl-es';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { Pipe, PipeTransform } from '@angular/core';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
@Pipe({
  name: 'truncate',
})
export class TruncatePipe implements PipeTransform {
  transform(value: string, limit = 100, ellipsis = '...'): string {
    return value && value.length > limit
      ? value.slice(0, limit) + ellipsis
      : value;
  }
}

@Component({
  selector: 'app-configuracion',
  imports: [
    MenuLateralComponent,
    RouterModule,
    CommonModule,
    NgxPaginationModule,
    FormsModule,
    MatIconModule,
    MatTooltipModule,
    MatInputModule,
    MatFormFieldModule,
    MatButtonModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatSortModule,
    MatSlideToggleModule,
  ],
  templateUrl: './configuracion.component.html',
  styleUrl: './configuracion.component.css',
})
export class ConfiguracionComponent implements OnInit {
  menuVisible = true;
  token: string | null = null;

  cargando: boolean = false;

  registrosActivados: boolean = false;
  nivelRegistros: number = 0;
  tiempoRetencion: number = 0;

  constructor(
    private http: HttpClient,
    private dialog: MatDialog,
    private router: Router
  ) {}
  ngOnInit(): void {
    this.token = localStorage.getItem('token');
    if (this.token) {
      this.obtenerConfiguracion();
    }
  }

guardarConfiguracion() {
  if (this.tiempoRetencion < 1) {
    this.showErrorPopup('El tiempo de retención minimo es 1 dia.');
    return;
  }
    if (this.tiempoRetencion === null || this.tiempoRetencion === undefined || this.tiempoRetencion === null) {
    this.showErrorPopup('El tiempo de retención no puede estar vacío.');
    return;
  }

  this.cargando = true;

  const headers = new HttpHeaders()
    .set('Authorization', `Bearer ${this.token}`)
    .set('Content-Type', 'application/json');

  const url = 'https://api.bioemach.cl/configuracion/editar';

  const updatedConfig = {
    configuracion_log_enable: this.registrosActivados,
    configuracion_nivel_log_api: this.nivelRegistros,
    configuracion_tiempo_retencion: this.tiempoRetencion,
  };

  this.http
    .put<any>(url, updatedConfig, { headers, observe: 'response' })
    .subscribe({
      next: (response) => {
        const statusCode = response.status;
        switch (statusCode) {
          case 200:
            this.showSuccessPopup(
              response.body?.mensaje ||
                'Configuración actualizada exitosamente.'
            );
            break;
          default:
            this.showErrorPopup(
              `Respuesta inesperada del servidor. Código: ${statusCode}`
            );
        }
      },
      error: (error) => {
        const statusCode = error.status;

        switch (statusCode) {
          case 400:
            this.showErrorPopup(
              error.error?.mensaje ||
                'Solicitud inválida. Verifique los datos ingresados.'
            );
            break;
          case 401:
            this.tokenInvalido();
            break;
          case 404:
            this.showErrorPopup('Configuración no encontrada.');
            break;
          case 500:
            this.showErrorPopup(
              'Error interno del servidor. Intente más tarde.'
            );
            break;
          default:
            this.showErrorPopup(
              `Error desconocido (${statusCode}). Contacte al área informática.`
            );
        }

        console.error('Error al guardar configuración:', error);
      },
      complete: () => {
        this.cargando = false;
      },
    });
}

  obtenerConfiguracion() {
    this.cargando = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );
    const url = 'https://api.bioemach.cl/configuracion';

    this.http.get<any>(url, { headers }).subscribe({
      next: (response) => {
        // Mapear datos al formulario
        this.registrosActivados = response.configuracion_log_enable;
        this.nivelRegistros = response.configuracion_nivel_log_api;
        this.tiempoRetencion = response.configuracion_tiempo_retencion;
        console.log(response);
      },
      error: (error) => {
        console.error('Error al obtener Configuración:', error);

        switch (error.status) {
          case 0:
            this.showErrorPopup(
              'No se pudo conectar con el servidor. Revise su conexión.'
            );
            break;
          case 401:
            this.tokenInvalido();
            break;
          case 403:
            this.showErrorPopup(
              'Acceso denegado. No tiene permisos para ver esta configuración.'
            );
            break;
          case 404:
            this.showErrorPopup(
              'No se encontró la configuración. Contacte al área informática.'
            );
            break;
          case 500:
            this.showErrorPopup(
              'Error interno del servidor. Intente más tarde o contacte soporte.'
            );
            break;
          default:
            this.showErrorPopup('Error inesperado. Código: ' + error.status);
            break;
        }
      },
      complete: () => {
        this.cargando = false;
      },
    });
  }
  /*--------------------*/
  /*      POPPUS        */
  /*--------------------*/

  onMenuToggle(state: boolean) {
    this.menuVisible = state;
  }

  showErrorPopup(message: string): void {
    this.dialog.open(ErrorPopupComponent, {
      data: { message },
    });
  }
  showSuccessPopup(message: string): void {
    this.dialog.open(SuccessPopupComponent, {
      data: { message },
    });
  }

  showInformationPopup(
    message: string
  ): MatDialogRef<InformationPopupComponent> {
    return this.dialog.open(InformationPopupComponent, {
      data: { message },
    });
  }

  tokenInvalido() {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
