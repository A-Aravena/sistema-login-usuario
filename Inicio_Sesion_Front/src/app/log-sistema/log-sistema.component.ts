import { CommonModule } from '@angular/common';
import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
  HttpParams,
} from '@angular/common/http';
import { Component, Pipe, PipeTransform, ViewChild } from '@angular/core';
import { FormsModule, NgModel } from '@angular/forms'; // Importar FormsModule para usar ngModel
import { MomentDateAdapter } from '@angular/material-moment-adapter';
import { MatButtonModule } from '@angular/material/button';
import {
  DateAdapter,
  MAT_DATE_FORMATS,
  MAT_DATE_LOCALE,
  MatNativeDateModule,
} from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import {
  MatPaginator,
  MatPaginatorIntl,
  MatPaginatorModule,
  PageEvent,
} from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterModule } from '@angular/router';
import * as moment from 'moment';
import { NgxPaginationModule } from 'ngx-pagination';
import { ErrorPopupComponent } from '../components/error-popup/error-popup.component';
import { InformationPopupComponent } from '../components/information-popup/information-popup.component';
import { MenuLateralComponent } from '../components/menu-lateral/menu-lateral.component';
import { SuccessPopupComponent } from '../components/success-popup/success-popup.component';
import { getSpanishPaginatorIntl } from '../utils/paginator-intl-es';

export const MY_DATE_FORMATS = {
  parse: {
    dateInput: 'DD-MM-YYYY',
  },
  display: {
    dateInput: 'DD-MM-YYYY',
    monthYearLabel: 'MMMM YYYY',
    dateA11yLabel: 'DD-MM-YYYY',
    monthYearA11yLabel: 'MMMM YYYY',
  },
};

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
  selector: 'app-log-sistema',
  providers: [
    { provide: MatPaginatorIntl, useValue: getSpanishPaginatorIntl() },
    {
      provide: DateAdapter,
      useClass: MomentDateAdapter,
      deps: [MAT_DATE_LOCALE],
    },
    { provide: MAT_DATE_FORMATS, useValue: MY_DATE_FORMATS },
  ],
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
    MatPaginatorModule,
    MatSortModule,
  ],
  templateUrl: './log-sistema.component.html',
  styleUrl: './log-sistema.component.css',
})
export class LogSistemaComponent {
  menuVisible = true;
  mostrarPopup = false;
  token: string | null = null;
  LogsApi: any[] = [];
  sortField: string | null = null;
  sortDirection: string = 'desc';
  totalItems: number = 0;
  pageSize: number = 5; 
  currentPage: number = 1;
  pageIndex = 0;
  pagesArray: (number | string)[] = [];
  ordenAscendente: boolean = false;
  filtrosActivos: boolean = false;
  accion = '';
  cambiosRealizado = '';
  fechaInicio: moment.Moment | null = null;
  fechaFin: moment.Moment | null = null;
  logSeleccionado: any = null;
  administradorId: number | null = null;
  cargando: boolean = false;
  administradorNombre: string = '';
  administradorApellido: string = '';
  mostrarPasswordEdit: boolean = false;
  displayedColumns: string[] = [
    'responsable',
    'log_sistema_accion',
    'log_sistema_cambios_realizados',
    'log_sistema_fecha',
  ];

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild('fechaInicioInput') fechaInicioInput: any;
  @ViewChild('fechaFinInput') fechaFinInput: any;
  @ViewChild('fechaInicioNgModel') fechaInicioNgModel: NgModel | undefined;
  @ViewChild('fechaFinNgModel') fechaFinNgModel: NgModel | undefined;

  constructor(
    private http: HttpClient,
    private dialog: MatDialog,
    private router: Router
  ) {}
  ngOnInit(): void {
    this.token = localStorage.getItem('token');
    if (this.token) {
      this.obtenerLogSistema(this.currentPage);
    }
  }

  limpiarFiltros() {
    this.cambiosRealizado = '';
    this.fechaInicio = null;
    this.fechaFin = null;
    this.filtrosActivos = false;
    this.accion = '';
    // Limpiar manualmente el input de texto
    if (this.fechaInicioInput) {
      this.fechaInicioInput.nativeElement.value = '';
    }
    if (this.fechaFinInput) {
      this.fechaFinInput.nativeElement.value = '';
    }

    // Restaurar estado del ngModel
    if (this.fechaInicioNgModel) {
      this.fechaInicioNgModel.control.markAsPristine();
      this.fechaInicioNgModel.control.markAsUntouched();
      this.fechaInicioNgModel.control.updateValueAndValidity();
    }
    if (this.fechaFinNgModel) {
      this.fechaFinNgModel.control.markAsPristine();
      this.fechaFinNgModel.control.markAsUntouched();
      this.fechaFinNgModel.control.updateValueAndValidity();
    }
    this.paginator.pageIndex = 0; // reset paginador visual
    this.sortField = null; // reset orden
    this.sortDirection = 'desc';
    this.obtenerLogSistema(1);
  }

  exportar() {
    const sinFiltros =
      !this.accion &&
      !this.cambiosRealizado &&
      !this.fechaInicio &&
      !this.fechaFin;

    if (sinFiltros) {
      const dialogRef = this.dialog.open(InformationPopupComponent, {
        data: {
          message:
            'No se aplicaron filtros. ¿Desea exportar todos los Registros?',
        },
      });

      dialogRef.afterClosed().subscribe((result) => {
        if (result) {
          this.realizarExportacion(); 
        }
      });
    } else {
      this.realizarExportacion(); 
    }
  }

  private realizarExportacion(): void {
    this.cargando = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );

    const formatDate = (date: Date | null): string => {
      if (!date) return '';
      const d = new Date(date);
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    };

    let params = new HttpParams();
    if (this.accion) params = params.set('accion', this.accion);
    if (this.cambiosRealizado)
      params = params.set('cambiosRealizado', this.cambiosRealizado);
    if (this.fechaInicio)
      params = params.set(
        'fechaInicio',
        formatDate(this.fechaInicio?.toDate() ?? null)
      );
    if (this.fechaFin)
      params = params.set(
        'fechaFin',
        formatDate(this.fechaFin?.toDate() ?? null)
      );

    const endpoint = 'https://api.bioemach.cl/logs/exportar/log_sistema';

    this.http
      .get(endpoint, {
        headers,
        params,
        responseType: 'blob',
        observe: 'response',
      })
      .subscribe({
        next: (response) => {
          switch (response.status) {
            case 200:
              if (response.body) {
                const blob = response.body;
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = 'log_sistema.csv';
                a.click();
                window.URL.revokeObjectURL(url);
              } else {
                this.showErrorPopup(
                  'El archivo recibido está vacío o no se pudo generar.'
                );
              }
              break;

            default:
              this.showErrorPopup(
                `Código de respuesta inesperado: ${response.status}`
              );
              break;
          }
          this.cargando = false;
        },
        error: (err) => {
          console.error('Error al exportar:', err);
          switch (err.status) {
            case 401:
              this.tokenInvalido();
              break;
            case 500:
              this.showErrorPopup('Error interno del servidor al exportar.');
              break;
            case 400:
              this.showErrorPopup('Error en los datos enviados.');
              break;
            default:
              this.showErrorPopup(
                'Error al exportar Logs Sistema.\nFavor ponerse en contacto con el área informática.'
              );
              break;
          }
          this.cargando = false;
        },
      });
  }

  aplicarFiltros(page: number = 1) {
    if (!this.token) return;

    this.cargando = true;
    this.filtrosActivos = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );

    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', this.pageSize.toString());

    if (this.accion?.trim()) {
      params = params.set('accion', this.accion.trim());
    }

    if (this.cambiosRealizado?.trim()) {
      params = params.set('cambiosRealizados', this.cambiosRealizado.trim());
    }

    if (this.fechaInicio) {
      params = params.set('fechaInicio', this.formatearFecha(this.fechaInicio));
    }

    if (this.fechaFin) {
      params = params.set('fechaFin', this.formatearFecha(this.fechaFin));
    }

    if (this.sortField) {
      params = params
        .set('sortField', this.sortField)
        .set('sortDirection', this.sortDirection);
    }

    this.http
      .get<any>('https://api.bioemach.cl/logs/log_sistema_filtro', {
        headers,
        params,
        observe: 'response', // <-- Necesario para obtener el status code en el .next también
      })
      .subscribe({
        next: (res) => {
          switch (res.status) {
            case 200:
              this.LogsApi = res.body.data;
              this.totalItems = res.body.totalItems;
              this.pageSize = res.body.pageSize;
              this.currentPage = res.body.currentPage;
              break;
            default:
              this.showErrorPopup(
                `Respuesta inesperada del servidor: código ${res.status}`
              );
          }
        },
        error: (error: HttpErrorResponse) => {
          switch (error.status) {
            case 400:
              this.showErrorPopup(
                'Solicitud incorrecta (400). Verifica los filtros.'
              );
              break;
            case 401:
              this.tokenInvalido();
              break;
            case 403:
              this.showErrorPopup('Acceso denegado (403). No tienes permisos.');
              break;
            case 404:
              this.showErrorPopup('Recurso no encontrado (404).');
              break;
            case 500:
              this.showErrorPopup('Error interno del servidor (500).');
              break;
            default:
              this.showErrorPopup(`Error desconocido (${error.status}).`);
          }
        },
        complete: () => {
          this.cargando = false;
        },
      });
  }

  obtenerLogSistema(page: number) {
    if (!this.token) return;

    this.cargando = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );

    let url = `https://api.bioemach.cl/logs/log_sistema?page=${page}&pageSize=${this.pageSize}`;

    if (this.sortField) {
      url += `&sortField=${this.sortField}&sortDirection=${this.sortDirection}`;
    }

    this.http.get<any>(url, { headers }).subscribe({
      next: (response) => {
        this.LogsApi = response.data;
        this.totalItems = response.totalItems;
        this.pageSize = response.pageSize;
        this.currentPage = response.currentPage;
      },
      error: (error) => {
        this.cargando = false;

        const status = error.status;
        let mensajeError = 'Error desconocido. Contacte al área informática.';

        switch (status) {
          case 0:
            mensajeError = 'No se pudo conectar con el servidor.';
            break;
          case 400:
            mensajeError =
              'Solicitud incorrecta. Verifique los parámetros enviados.';
            break;
          case 401:
            this.tokenInvalido();
            break;
          case 402:
            mensajeError = 'Pago requerido. Este servicio no está habilitado.';
            break;
          case 403:
            mensajeError =
              'Prohibido. No tiene permisos para acceder a este recurso.';
            break;
          case 404:
            mensajeError = 'Recurso no encontrado.';
            break;
          case 500:
            mensajeError = 'Error interno del servidor.';
            break;
        }

        console.error('Error al obtener Registros Log Sistema:', error);
      },
      complete: () => {
        this.cargando = false;
      },
    });
  }

  ngAfterViewInit() {
    this.sort.sortChange.subscribe((sort) => {
      this.sortField = sort.active;
      this.sortDirection = sort.direction ? sort.direction : 'desc';
      this.paginator.pageIndex = 0;
      if (this.filtrosActivos) {
        this.aplicarFiltros(1);
      } else {
        this.obtenerLogSistema(1);
      }
    });

    this.paginator.page.subscribe((pageEvent: PageEvent) => {
      this.pageIndex = pageEvent.pageIndex;
      this.pageSize = pageEvent.pageSize;

      if (this.filtrosActivos) {
        this.aplicarFiltros(this.pageIndex + 1);
      } else {
        this.obtenerLogSistema(this.pageIndex + 1);
      }
    });

    if (this.filtrosActivos) {
      this.aplicarFiltros(1);
    } else {
      // this.obtenerLogSistema(1);
    }
  }

  // Función para manejar el cambio de tamaño de página
  onPageSizeChange() {
    this.currentPage = 1; // resetear a primera página
    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerLogSistema(this.currentPage);
    }
  }
  onPageChange(event: PageEvent) {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;

    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerLogSistema(this.currentPage);
    }
  }

  formatearFecha(fecha: moment.Moment): string {
    return fecha.format('YYYY-MM-DD');
  }

  /*--------------------*/
  /*      POPPUS        */
  /*--------------------*/

  onMenuToggle(state: boolean) {
    this.menuVisible = state;
  }

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize);
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
