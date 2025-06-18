import { CommonModule } from '@angular/common';
import {
  HttpClient,
  HttpHeaders,
  HttpParams,
  HttpResponse,
} from '@angular/common/http';
import { Component, Pipe, PipeTransform, ViewChild } from '@angular/core';
import { FormsModule, NgModel } from '@angular/forms'; // Importar FormsModule para usar ngModel
import { MomentDateAdapter } from '@angular/material-moment-adapter';
import { MatButtonModule } from '@angular/material/button';
import { DateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE, MatNativeDateModule } from '@angular/material/core';
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
  selector: 'app-log-api',
  providers: [
    { provide: MatPaginatorIntl, useValue: getSpanishPaginatorIntl() },
    { provide: DateAdapter, useClass: MomentDateAdapter, deps: [MAT_DATE_LOCALE] },
    { provide: MAT_DATE_FORMATS, useValue: MY_DATE_FORMATS }
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
    TruncatePipe,
  ],
  templateUrl: './log-api.component.html',
  styleUrl: './log-api.component.css',
})
export class LogApiComponent {
  menuVisible = true;
  mostrarPopup = false;

  token: string | null = null;

  LogsApi: any[] = [];

  sortField: string | null = null;
  sortDirection: string = 'desc';
  totalItems: number = 0;
  pageSize: number = 5; // Por defecto, 5 ítems por página
  currentPage: number = 1;
  pageIndex = 0;

  pagesArray: (number | string)[] = [];

  ordenAscendente: boolean = false;

  // Campos de filtro
  filtrosActivos: boolean = false;
  metodo = '';
  url = '';
  tipoRespuesta = '';
  tiempoEjecucion = '';
  paramEntrada = '';
  paramSalida = '';
  query = '';
fechaInicio: moment.Moment | null = null;
fechaFin: moment.Moment | null = null;

  logSeleccionado: any = null;

  administradorId: number | null = null;
  cargando: boolean = false;

  administradorNombre: string = '';
  administradorApellido: string = '';

  modo: 'crear' | 'editar' = 'crear';
  mostrarPasswordEdit: boolean = false;
 interaccionDentro = false;
  displayedColumns: string[] = [
    'log_api_metodo',
    'log_api_url',
    'log_api_tipo_respuesta',
    'log_api_tiempo_ejecucion',
    'log_api_fecha',
    'log_api_parametros_entrada',
    'log_api_parametros_salida',
    'log_api_query',

    'acciones',
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
      this.obtenerLogapi(this.currentPage);
    }
  }

    limpiarFiltros() {
    this.metodo = '';
    this.url = '';
    this.tipoRespuesta = '';
    this.tiempoEjecucion = '';
    this.paramEntrada = '';
    this.paramSalida = '';
    this.query = '';
    this.fechaInicio = null;
    this.fechaFin = null;
    this.filtrosActivos = false;
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
    this.obtenerLogapi(1);
  }



  onInsidePointerDown(event: PointerEvent) {
  this.interaccionDentro = true;
}

onModalPointerUp(event: PointerEvent) {
  if (!this.interaccionDentro) {
    this.cerrarPopup();
  }
  // resetear para la próxima interacción
  this.interaccionDentro = false;
}
  exportar() {
    const sinFiltros =
      !this.metodo &&
      !this.url &&
      !this.tipoRespuesta &&
      !this.tiempoEjecucion &&
      !this.paramEntrada &&
      !this.paramSalida &&
      !this.query &&
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
          this.realizarExportacion(); // si el usuario acepta
        }
      });
    } else {
      this.realizarExportacion(); // si hay filtros, exporta directamente
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

    if (this.metodo) params = params.set('metodo', this.metodo);
    if (this.url) params = params.set('url', this.url);
    if (this.tipoRespuesta)
      params = params.set('tipoRespuesta', this.tipoRespuesta);
    if (this.fechaInicio)
      params = params.set('fechaInicio', formatDate(this.fechaInicio?.toDate() ?? null))
    if (this.fechaFin)
      params = params.set('fechaFin', formatDate(this.fechaFin?.toDate() ?? null))
    if (this.paramEntrada)
      params = params.set('parametrosEntrada', this.paramEntrada);
    if (this.paramSalida)
      params = params.set('parametrosSalida', this.paramSalida);
    if (this.query) params = params.set('query', this.query);

    const endpoint = 'https://api.bioemach.cl/logs/exportar/log_api';

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
              // Respuesta exitosa - archivo blob
              const url = window.URL.createObjectURL(response.body!);
              const a = document.createElement('a');
              a.href = url;
              a.download = 'log_api.csv';
              a.click();
              window.URL.revokeObjectURL(url);
              break;
            default:
              this.showErrorPopup(
                `Error inesperado. Código: ${response.status}`
              );
              break;
          }
        },
        error: (err) => {
            switch (err.status) {

            case 401:
              this.tokenInvalido();
              break;
            case 400:
              // Bad request
              this.showErrorPopup(
                'Solicitud inválida. Revise los parámetros enviados.'
              );
              break;
            case 500:
              // Error interno
              this.showErrorPopup(
                'Error interno del servidor. Intente más tarde.'
              );
              break;
            default:
              this.showErrorPopup(
                `Error inesperado. Código: ${err.status}`
              );
              break;
          }
        },
        complete: () => {
          this.cargando = false;
        },
      });
  }

  aplicarFiltros(page: number = 1) {
    this.cargando = true;
    this.filtrosActivos = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );

    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', this.pageSize.toString());

    if (this.metodo?.trim()) {
      params = params.set('metodo', this.metodo.trim());
    }
    if (this.url?.trim()) {
      params = params.set('url', this.url.trim());
    }
    if (this.tipoRespuesta?.trim()) {
      params = params.set('tipoRespuesta', this.tipoRespuesta.trim());
    }
    if (this.tiempoEjecucion?.trim()) {
      params = params.set('tiempoEjecucion', this.tiempoEjecucion.trim());
    }
    if (this.paramEntrada?.trim()) {
      params = params.set('parametrosEntrada', this.paramEntrada.trim());
    }
    if (this.paramSalida?.trim()) {
      params = params.set('parametrosSalida', this.paramSalida.trim());
    }
    if (this.query?.trim()) {
      params = params.set('queryApi', this.query.trim());
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
      .get<any>('https://api.bioemach.cl/logs/log_api_filtro', {
        headers,
        params,
        observe: 'response', // para obtener status y todo el response
      })
      .subscribe({
        next: (response: HttpResponse<any>) => {
          switch (response.status) {
            case 200:
              this.LogsApi = response.body.data;
              this.totalItems = response.body.totalItems;
              this.pageSize = response.body.pageSize;
              this.currentPage = response.body.currentPage;
              break;
            default:
              this.showErrorPopup(`Error inesperado: ${response.status}`);
              break;
          }
        },
        error: (error) => {
          // Aquí capturas errores que no son 2xx ni 4xx capturados arriba, por ejemplo fallos de red
          switch (error.status) {
            case 400:
              this.showErrorPopup('Solicitud incorrecta. Revise los filtros.');
              break;
            case 401:
              this.tokenInvalido();
              break;
            case 500:
              this.showErrorPopup(
                'Error interno del servidor. Intente más tarde.'
              );
              break;
            default:
              this.showErrorPopup(`Error inesperado: ${error.status}`);
              break;
          }
        },
        complete: () => {
          this.cargando = false;
        },
      });
  }

  obtenerLogapi(page: number) {
    this.cargando = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );
    let url = `https://api.bioemach.cl/logs/log_api?page=${page}&pageSize=${this.pageSize}`;

    if (this.sortField) {
      url += `&sortField=${this.sortField}&sortDirection=${this.sortDirection}`;
    }

    this.http.get<any>(url, { headers, observe: 'response' }).subscribe({
      next: (response) => {
        switch (response.status) {
          case 200:
            this.LogsApi = response.body.data;
            this.totalItems = response.body.totalItems;
            this.pageSize = response.body.pageSize;
            this.currentPage = response.body.currentPage;
            break;

          default:
            this.showErrorPopup(`Error inesperado: ${response.status}`);
            break;
        }
      },
      error: (error) => {
        switch (error.status) {
          case 400:
            this.showErrorPopup(
              'Solicitud incorrecta. Revise los parámetros enviados.'
            );
            break;

          case 401:
            this.tokenInvalido();
            break;

          case 402:
            this.showErrorPopup(
              'Pago requerido. Contacte al área administrativa.'
            );
            break;

          case 500:
            this.showErrorPopup(
              'Error interno del servidor. Intente más tarde.'
            );
            break;

          default:
            this.showErrorPopup(`Error inesperado: ${error.status}`);
            break;
        }
      },
      complete: () => {
        this.cargando = false;
      },
    });
  }

  borrarRegistros() {
    const dialogRef = this.dialog.open(InformationPopupComponent, {
      data: {
        message:
          '¿Desea borrar todos los registros? \n Esta acción no se puede deshacer',
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.ejecutarBorrado();
      }
    });
  }

  ejecutarBorrado(): void {
    if (!this.token) return;

    this.cargando = true;

    const headers = new HttpHeaders({
      Authorization: this.token,
    });

    this.http
      .delete<any>('https://api.bioemach.cl/logs/log_api/borrar', {
        headers,
        observe: 'response',
      })
      .subscribe({
        next: (response) => {
          switch (response.status) {
            case 200:
              this.showSuccessPopup(
                'Borrado de Registros Exitoso \n' + response.body.mensaje
              );
              this.obtenerLogapi(this.currentPage); // recarga
              break;

            default:
              this.showErrorPopup(`Respuesta inesperada: ${response.status}`);
              break;
          }
          this.cargando = false;
        },
        error: (error) => {
          console.error('Error al borrar registros:', error);
          switch (error.status) {
            case 401:
              this.tokenInvalido();
              break;
            case 500:
              this.showErrorPopup(
                'Error interno del servidor al borrar registros.'
              );
              break;
            default:
              this.showErrorPopup(
                'Ocurrió un error al intentar borrar los registros.'
              );
              break;
          }
          this.cargando = false;
        },
      });
  }
  verLogApi(log: any) {
    // Formatear fecha dd-mm-aaaa con ceros a la izquierda
    const fechaOriginal = log.log_api_fecha;

    const fechaObj = new Date(fechaOriginal);

    const dia = ('0' + fechaObj.getDate()).slice(-2);
    const mes = ('0' + (fechaObj.getMonth() + 1)).slice(-2);
    const anio = fechaObj.getFullYear();
    const fechaFormateada = `${dia}-${mes}-${anio}`;

    // Formatear tiempo de ejecución a 2 decimales (asegurarse que es número)
    const tiempo = Number(log.log_api_tiempo_ejecucion);
    const tiempoFormateado = isNaN(tiempo)
      ? log.log_api_tiempo_ejecucion
      : tiempo.toFixed(2);

    this.logSeleccionado = {
      metodo: log.log_api_metodo,
      url: log.log_api_url,
      tipo_respuesta: log.log_api_tipo_respuesta,
      tiempo_ejecucion: tiempoFormateado, // <-- aquí tiempo con 2 decimales
      parametros_entrada: log.log_api_parametros_entrada,
      parametros_salida: log.log_api_parametros_salida,
      query: log.log_api_query,
      fecha: fechaFormateada,
    };
    this.mostrarPopup = true;
  }

  cerrarPopup() {
    this.mostrarPopup = false;
    this.administradorId = null;
  }


  ngAfterViewInit() {
    this.sort.sortChange.subscribe((sort) => {
      this.sortField = sort.active;
      this.sortDirection = sort.direction ? sort.direction : 'desc';
      this.paginator.pageIndex = 0;
      if (this.filtrosActivos) {
        this.aplicarFiltros(1);
      } else {
        this.obtenerLogapi(1);
      }
    });

    this.paginator.page.subscribe((pageEvent: PageEvent) => {
      this.pageIndex = pageEvent.pageIndex;
      this.pageSize = pageEvent.pageSize;

      if (this.filtrosActivos) {
        this.aplicarFiltros(this.pageIndex + 1);
      } else {
        this.obtenerLogapi(this.pageIndex + 1);
      }
    });

    if (this.filtrosActivos) {
      this.aplicarFiltros(1);
    } else {
      //this.obtenerLogapi(1);
    }
  }

  // Función para manejar el cambio de tamaño de página
  onPageSizeChange() {
    this.currentPage = 1; // resetear a primera página
    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerLogapi(this.currentPage);
    }
  }
  onPageChange(event: PageEvent) {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;

    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerLogapi(this.currentPage);
    }
  }

formatearFecha(fecha: moment.Moment): string {
  return fecha.format('YYYY-MM-DD');
}
  truncate(value: number): string {
    if (value === null || value === undefined) return '-';
    const truncated = Math.floor(value * 100) / 100;
    return truncated.toFixed(2); // asegura 2 decimales fijos
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
