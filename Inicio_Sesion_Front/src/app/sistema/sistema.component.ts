import { Component, OnInit } from '@angular/core';
import { MenuLateralComponent } from '../components/menu-lateral/menu-lateral.component';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
  HttpParams,
} from '@angular/common/http';
import { NgxPaginationModule } from 'ngx-pagination';
import { FormsModule, NgModel } from '@angular/forms'; // Importar FormsModule para usar ngModel
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
import {
  DateAdapter,
  MAT_DATE_FORMATS,
  MAT_DATE_LOCALE,
  MatNativeDateModule,
} from '@angular/material/core';

import { AfterViewInit, ViewChild } from '@angular/core';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { getSpanishPaginatorIntl } from '../utils/paginator-intl-es';
import {
  MatSlideToggleChange,
  MatSlideToggleModule,
} from '@angular/material/slide-toggle';
import { Router } from '@angular/router';

import {
  MatPaginator,
  MatPaginatorIntl,
  MatPaginatorModule,
  PageEvent,
} from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MomentDateAdapter } from '@angular/material-moment-adapter';
import * as moment from 'moment';

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

@Component({
  selector: 'app-sistema',
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
    MatSlideToggleModule,
  ],
  templateUrl: './sistema.component.html',
  styleUrl: './sistema.component.css',
})
export class SistemaComponent {
  menuVisible = true;
  mostrarPopup = false;

  token: string | null = null;

  sistemas: any[] = [];

  sortField: string | null = null;
  sortDirection: string = 'desc';
  totalItems: number = 0;
  pageSize: number = 5; // Por defecto, 5 ítems por página
  currentPage: number = 1;
  pageIndex = 0;

  pagesArray: (number | string)[] = [];

  campoOrden: string = '';
  ordenAscendente: boolean = false;

  // Campos de filtro
  filtrosActivos: boolean = false;
  nombre = '';
  sistema_key = '';
  sistema_estado = '';

  fechaInicio: moment.Moment | null = null;
  fechaFin: moment.Moment | null = null;
  sistemaId: number | null = null;
  cargando: boolean = false;
  interaccionDentro = false;
  modo: 'crear' | 'editar' = 'crear';
  mostrarPasswordEdit: boolean = false;
  formularioEdit = {
    id: '',
    nombre: '',
    sistema_key: '',
    sistema_estado: '',
  };

  displayedColumns: string[] = [
    'sistema_nombre',
    'sistema_key',
    'sistema_estado',
    'created_at',
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
      this.obtenerSistemas(this.currentPage);
    }
  }


    limpiarFiltros() {
    this.nombre = '';
    this.sistema_key = '';
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
    this.obtenerSistemas(1);
  }


  editarSistema() {
    if (!this.token) return;

    this.cargando = true;

    const headers = new HttpHeaders({
      Authorization: this.token,
      'Content-Type': 'application/json',
    });

    const body = {
      sistema_nombre: this.formularioEdit.nombre,
      sistema_key: this.formularioEdit.sistema_key,
      sistema_estado: this.formularioEdit.sistema_estado,
    };

    this.http
      .put<any>(
        `https://api.bioemach.cl/sistema/edit/${this.sistemaId}`,
        body,
        { headers }
      )
      .subscribe({
        next: (response) => {
          console.log('Respuesta exitosa:', response);
          this.cargando = false;
          this.cerrarPopupEdit();
          this.aplicarFiltros(this.currentPage);
          this.showSuccessPopup(
            response.mensaje || 'Sistema editado exitosamente'
          );
        },
        error: (error) => {
          this.cargando = false;

          const mensaje = error.error?.mensaje || 'Error desconocido';
          console.error('Error al editar Sistema:', mensaje);

          switch (error.status) {
            case 400:
              this.showErrorPopup(mensaje);
              break;
            case 401:
              this.tokenInvalido();
              break;
            case 404:
              this.showErrorPopup('Sistema no encontrado');
              break;
            case 500:
              this.showErrorPopup(
                'Error Interno del Servidor. Contacte al área informática.'
              );
              break;
            default:
              this.showErrorPopup(
                'Ocurrió un error inesperado. Intente nuevamente más tarde.'
              );
          }
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

    if (this.sortField) {
      params = params
        .set('sortField', this.sortField)
        .set('sortDirection', this.sortDirection);
    }

    if (this.nombre.trim() !== '')
      params = params.set('sistemaNombre', this.nombre.trim());
    if (this.sistema_key.trim() !== '')
      params = params.set('sistemaKey', this.sistema_key.trim());
    if (
      this.sistema_estado.trim() !== '' &&
      this.sistema_estado.trim() !== 'todos'
    ) {
      params = params.set('sistemaEstado', this.sistema_estado.trim());
    }

    if (this.fechaInicio) {
      params = params.set('fechaInicio', this.formatearFecha(this.fechaInicio));
    }
    if (this.fechaFin) {
      params = params.set('fechaFin', this.formatearFecha(this.fechaFin));
    }

    this.http
      .get<any>('https://api.bioemach.cl/sistema/filtrar', {
        headers,
        params,
      })
      .subscribe({
        next: (response) => {
          this.sistemas = response.data;
          this.totalItems = response.totalItems;
          this.pageSize = response.pageSize;
          this.currentPage = response.currentPage;
        },
        error: (error: HttpErrorResponse) => {
          this.cargando = false;
          switch (error.status) {
            case 400:
              this.showErrorPopup(
                'Solicitud incorrecta. Revise los filtros ingresados.'
              );
              break;
            case 401:
              this.tokenInvalido();
              break;
            case 403:
              this.showErrorPopup(
                'Acceso denegado. No tiene permisos para realizar esta acción.'
              );
              break;
            case 404:
              this.showErrorPopup(
                'Recurso no encontrado. Contacte al área informática.'
              );
              break;
            case 500:
              this.showErrorPopup(
                'Error interno del servidor. Intente más tarde.'
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



  obtenerSistemas(page: number) {
    this.cargando = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );

    let url = `https://api.bioemach.cl/sistema?page=${page}&pageSize=${this.pageSize}`;

    if (this.sortField) {
      url += `&sortField=${this.sortField}&sortDirection=${this.sortDirection}`;
    }

    this.http.get<any>(url, { headers, observe: 'response' }).subscribe({
      next: (response) => {
        switch (response.status) {
          case 200:
            // La API responde con un objeto que contiene data, totalItems, etc.
            this.sistemas = response.body.data;
            this.totalItems = response.body.totalItems;
            this.pageSize = response.body.pageSize;
            this.currentPage = response.body.page;
            this.cargando = false;
            break;

          case 204: // No content, por ejemplo
            this.sistemas = [];
            this.totalItems = 0;
            this.cargando = false;
            break;

          default:
            this.cargando = false;
            this.showErrorPopup('Respuesta inesperada del servidor.');
            break;
        }
      },
      error: (error) => {
        this.cargando = false;
        switch (error.status) {
          case 400:
            this.showErrorPopup(
              error.error?.mensaje || 'Solicitud incorrecta.'
            );
            break;
          case 401:
            this.tokenInvalido();
            break;
          case 402:
            this.showErrorPopup(error.error?.mensaje || 'Pago requerido.');
            break;
          case 403:
            this.showErrorPopup(error.error?.mensaje || 'Prohibido.');
            break;
          case 404:
            this.showErrorPopup(
              error.error?.mensaje || 'Recurso no encontrado.'
            );
            break;
          case 500:
            this.showErrorPopup(
              'Error interno del servidor. Contacte al área informática.'
            );
            break;
          default:
            this.showErrorPopup('Error desconocido. Intente nuevamente.');
            break;
        }
        console.error('Error al obtener Sistemas:', error);
      },
      complete: () => {
        this.cargando = false;
      },
    });
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

  cambiarEstado(event: MatSlideToggleChange, sistema: any): void {
    event.source.checked = !event.checked;

    const activar = sistema.sistema_estado !== 1;
    const nuevoEstado = activar ? 1 : 0;

    const mensaje = activar
      ? '¿Desea activar este sistema?'
      : '¿Desea desactivar este sistema? Todas las sesiones se cerrarán y los usuarios no podrán iniciar sesión hasta que se reactive.';

    const dialogRef = this.dialog.open(InformationPopupComponent, {
      data: { message: mensaje },
    });

    dialogRef.afterClosed().subscribe((confirmado) => {
      if (confirmado) {
        this.cargando = true;

        if (!this.token) return;

        const headers = new HttpHeaders({
          Authorization: this.token,
          'Content-Type': 'application/json',
        });

        const body = { sistema_estado: nuevoEstado };

        this.http
          .put<any>(
            `https://api.bioemach.cl/sistema/cambiar-estado/${sistema.sistema_id}`,
            body,
            { headers }
          )
          .subscribe({
            next: (response) => {
              this.cargando = false;
              this.showSuccessPopup(
                response?.mensaje || 'Cambio de estado realizado correctamente'
              );

              // Actualizar el estado visual del toggle
              event.source.checked = activar;
              sistema.sistema_estado = nuevoEstado;
              this.aplicarFiltros(1);
            },
            error: (error) => {
              this.cargando = false;
              console.error('Error al cambiar estado del sistema:', error);

              // Revertir toggle
              event.source.checked = !activar;

              switch (error.status) {
                case 0:
                  this.showErrorPopup(
                    'No se pudo conectar con el servidor. Verifique su conexión.'
                  );
                  break;
                case 400:
                  this.showErrorPopup(
                    error.error?.mensaje || 'Solicitud incorrecta.'
                  );
                  break;
                case 401:
                  this.tokenInvalido();
                  break;
                case 404:
                  this.showErrorPopup(
                    error.error?.mensaje ||
                      'El sistema especificado no fue encontrado.'
                  );
                  break;
                case 500:
                  this.showErrorPopup(
                    error.error?.mensaje || 'Error interno del servidor.'
                  );
                  break;
                default:
                  this.showErrorPopup(
                    'Ocurrió un error inesperado al intentar cambiar el estado.'
                  );
                  break;
              }
            },
          });
      } else {
        event.source.checked = !activar;
      }
    });
  }

  exportar() {
    const sinFiltros =
      !this.nombre && !this.sistema_key && !this.fechaInicio && !this.fechaFin;

    if (sinFiltros) {
      const dialogRef = this.dialog.open(InformationPopupComponent, {
        data: {
          message:
            'No se aplicaron filtros. ¿Desea exportar todos los sistemas?',
        },
      });

      dialogRef.afterClosed().subscribe((result) => {
        if (result) {
          this.realizarExportacion(); // si el usuario acepta
        }
        // si el usuario cancela, no se hace nada
      });
    } else {
      this.realizarExportacion(); // si hay filtros, exporta directamente
    }
  }

  realizarExportacion() {
    this.cargando = true; // Activa spinner y bloquea botones

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );

    const endpoint = 'https://api.bioemach.cl/sistema/exportar/sistema';

    this.http.get(endpoint, { headers, responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'sistemas.xlsx';
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.cargando = false;
        console.error('Error al exportar:', err);

        switch (err.status) {
          case 0:
            this.showErrorPopup(
              'No se pudo conectar con el servidor. Verifica tu conexión de red.'
            );
            break;
          case 400:
            this.showErrorPopup(
              'Solicitud incorrecta. Contacte al área informática.'
            );
            break;
          case 401:
            this.tokenInvalido();
            break;
          case 403:
            this.showErrorPopup(
              'Acceso denegado. No tiene permisos para exportar.'
            );
            break;
          case 404:
            this.showErrorPopup(
              'Recurso no encontrado. El endpoint puede estar mal configurado.'
            );
            break;
          case 500:
            this.showErrorPopup(
              'Error interno del servidor. Contacte al área informática.'
            );
            break;
          default:
            this.showErrorPopup('Error inesperado al exportar Sistemas.');
            break;
        }
      },
      complete: () => {
        this.cargando = false;
      },
    });
  }

  borrarSistema(sistemaId: number): void {
    const dialogRef = this.dialog.open(InformationPopupComponent, {
      data: {
        message:
          '¿Está seguro que desea eliminar este sistema? Esta acción eliminará los Usuarios Asociados\n Esta acción no se puede deshacer.',
      },
    });

    dialogRef.afterClosed().subscribe((confirmado) => {
      if (confirmado) {
        this.cargando = true;
        if (!this.token) return;

        const headers = new HttpHeaders({
          Authorization: this.token,
          'Content-Type': 'application/json',
        });

        this.http
          .delete<any>(`https://api.bioemach.cl/sistema/${sistemaId}`, {
            headers,
            observe: 'response', // Observar la respuesta completa para obtener el código de estado
          })
          .subscribe({
            next: (response) => {
              this.cargando = false;
              switch (response.status) {
                case 200:
                  this.showSuccessPopup('Sistema Eliminado Exitosamente');
                  this.obtenerSistemas(1);
                  break;
                case 401:
                  this.showErrorPopup(
                    'Token no autorizado o no pertenece a un administrador'
                  );
                  break;
                case 404:
                  this.showErrorPopup('Sistema no encontrado');
                  break;
                default:
                  this.showErrorPopup('Ocurrió un error inesperado');
                  break;
              }
            },
            error: (error) => {
              this.cargando = false;
              switch (error.status) {
                case 400:
                  this.showErrorPopup('Solicitud incorrecta');
                  break;
                case 401:
                  this.tokenInvalido();
                  break;
                case 404:
                  this.showErrorPopup('Sistema no encontrado');
                  break;
                case 500:
                  this.showErrorPopup(
                    'Error interno del servidor. Inténtelo nuevamente más tarde.'
                  );
                  break;
                default:
                  this.showErrorPopup('Ocurrió un error inesperado');
                  break;
              }
            },
          });
      }
    });
  }

  crearSistema() {
    if (!this.token) return;

    this.cargando = true;

    const headers = new HttpHeaders({
      Authorization: this.token,
      'Content-Type': 'application/json',
    });

    const body = {
      sistema_nombre: this.formularioEdit.nombre,
      sistema_estado: this.formularioEdit.sistema_estado,
    };

    this.http
      .post<any>(`https://api.bioemach.cl/sistema/crear-sistema`, body, {
        headers,
        observe: 'response', // para acceder al código status en la respuesta
      })
      .subscribe({
        next: (response) => {
          switch (response.status) {
            case 201:
              console.log('Sistema creado:', response.body);
              this.cargando = false;
              this.cerrarPopupEdit();
              this.aplicarFiltros(this.currentPage); // Refresca la lista
              this.showSuccessPopup('Sistema creado exitosamente');
              break;
            default:
              this.cargando = false;
              this.showErrorPopup('Respuesta inesperada del servidor.');
          }
        },
        error: (error) => {
          this.cargando = false;
          switch (error.status) {
            case 400:
              this.showErrorPopup(error.error?.mensaje || 'Datos inválidos');
              break;
            case 401:
              this.tokenInvalido();
              break;
            case 409:
              this.showErrorPopup('El sistema ya existe con ese nombre.');
              break;
            case 500:
              this.showErrorPopup(
                'Error interno del servidor. Contacte al área informática.'
              );
              break;
            default:
              this.showErrorPopup('Error desconocido. Intente nuevamente.');
              break;
          }
          console.error('Error al crear Sistema:', error);
        },
      });
  }

  


  async generarSistemaKey() {
    const uuid = crypto.randomUUID(); // Generar un UUID aleatorio

    const encoder = new TextEncoder();
    const data = encoder.encode(uuid);
    const hashBuffer = await crypto.subtle.digest('SHA-256', data);

    // Convertir el resultado (ArrayBuffer) a hex string
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    const hashHex = hashArray
      .map((b) => b.toString(16).padStart(2, '0'))
      .join('');

    // Asignar el resultado al formulario
    this.formularioEdit.sistema_key = hashHex;
  }

  ngAfterViewInit() {
    this.sort.sortChange.subscribe((sort) => {
      this.sortField = sort.active;
      this.sortDirection = sort.direction ? sort.direction : 'desc';
      this.paginator.pageIndex = 0;
      if (this.filtrosActivos) {
        this.aplicarFiltros(1);
      } else {
        this.obtenerSistemas(1);
      }
    });

    this.paginator.page.subscribe((pageEvent: PageEvent) => {
      this.pageIndex = pageEvent.pageIndex;
      this.pageSize = pageEvent.pageSize;

      if (this.filtrosActivos) {
        this.aplicarFiltros(this.pageIndex + 1);
      } else {
        this.obtenerSistemas(this.pageIndex + 1);
      }
    });

    if (this.filtrosActivos) {
      this.aplicarFiltros(1);
    } else {
      //this.obtenerSistemas(1);
    }
  }

  // Función para manejar el cambio de tamaño de página
  onPageSizeChange() {
    this.currentPage = 1; // resetear a primera página
    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerSistemas(this.currentPage);
    }
  }
  onPageChange(event: PageEvent) {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;

    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerSistemas(this.currentPage);
    }
  }

  formatearFecha(fecha: moment.Moment): string {
    return fecha.format('YYYY-MM-DD');
  }

  /*--------------------*/
  /*      POPPUS        */
  /*--------------------*/

  abrirEdit(sistemaId: number) {
    // Buscar el administrador en la lista actual
    console.log('Sistema SELECIONADO :' + sistemaId);
    this.modo = 'editar';

    const sistemaSeleccionado = this.sistemas.find(
      (sistema) => sistema.sistema_id === sistemaId
    );

    if (sistemaSeleccionado) {
      // Cargar los datos en el formulario de edición
      this.formularioEdit.nombre = sistemaSeleccionado.sistema_nombre || '';
      this.formularioEdit.sistema_key = sistemaSeleccionado.sistema_key || '';
      this.formularioEdit.sistema_estado = sistemaSeleccionado.sistema_estado;
      this.formularioEdit.id = sistemaSeleccionado.sistema_id;

      this.sistemaId = sistemaId; // guardar id para editar

      // Mostrar el modal
      this.mostrarPopup = true;
    } else {
      this.showErrorPopup('No se encontró el Sistema seleccionado.');
    }
  }

  cerrarPopupEdit() {
    this.mostrarPopup = false;
    this.sistemaId = null;
    this.formularioEdit = {
      nombre: '',
      sistema_key: '',
      sistema_estado: '',
      id: '',
    };
  }

  cerrarPopup() {
    this.mostrarPopup = false;
    this.sistemaId = null;
  }

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

  abrirCrear() {
    this.modo = 'crear';
    this.formularioEdit = {
      nombre: '',
      sistema_key: '',
      sistema_estado: '',
      id: '',
    };
    this.mostrarPopup = true;
  }
  tokenInvalido() {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
