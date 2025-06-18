import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule, NgModel } from '@angular/forms'; // Importar FormsModule para usar ngModel
import { MatIconModule } from '@angular/material/icon';
import { Router, RouterModule } from '@angular/router';
import { NgxPaginationModule } from 'ngx-pagination';
import { ErrorPopupComponent } from '../components/error-popup/error-popup.component';
import { MenuLateralComponent } from '../components/menu-lateral/menu-lateral.component';
import { SuccessPopupComponent } from '../components/success-popup/success-popup.component';

import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { InformationPopupComponent } from '../components/information-popup/information-popup.component';

import { MatButtonModule } from '@angular/material/button';
import {
  DateAdapter,
  MAT_DATE_FORMATS,
  MAT_DATE_LOCALE,
  MatNativeDateModule,
} from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';

import { ViewChild } from '@angular/core';
import { MomentDateAdapter } from '@angular/material-moment-adapter';
import {
  MatPaginator,
  MatPaginatorIntl,
  MatPaginatorModule,
  PageEvent,
} from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import * as moment from 'moment';
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

@Component({
  selector: 'app-administrador',
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
  templateUrl: './administrador.component.html',
  styleUrls: ['./administrador.component.css'],
})
export class AdministradorComponent {
  menuVisible = true;
  mostrarPopup = false;

  token: string | null = null;

  administradores: any[] = [];

  sortField: string | null = null;
  sortDirection: string = 'desc';
  totalItems: number = 0;
  pageSize: number = 5; // Por defecto, 5 ítems por página
  currentPage: number = 1;
  pageIndex = 0;

  // Campos de filtro
  filtrosActivos: boolean = false;
  nombre = '';
  apellido = '';
  email = '';
  telefono = '';
  fechaInicio: moment.Moment | null = null;
  fechaFin: moment.Moment | null = null;

  administradorId: number | null = null;
  cargando: boolean = false;

  modo: 'crear' | 'editar' = 'crear';
  mostrarPasswordEdit: boolean = false;
  interaccionDentro = false;

  formularioEdit = {
    id: '',
    nombres: '',
    apellidos: '',
    email: '',
    telefono: '',
    contrasena: '',
  };
  displayedColumns: string[] = [
    'administrador_nombres',
    'administrador_apellidos',
    'administrador_email',
    'administrador_fono',
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
      this.obtenerAdministradores(this.currentPage);
     
    }
  }
limpiarFiltros() {
  this.nombre = '';
  this.apellido = '';
  this.email = '';
  this.telefono = '';
  this.fechaInicio = null;
  this.fechaFin = null;

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

  this.filtrosActivos = false;
  this.paginator.pageIndex = 0;
  this.sortField = null;
  this.sortDirection = 'desc';
  this.obtenerAdministradores(1);
}

    getLastPageIndex(): number {
    return Math.max(0, Math.ceil(this.totalItems / this.pageSize) - 1);
  }
irPrimeraPagina() {
  this.paginator.firstPage();
  this.obtenerAdministradores(1);
}

irUltimaPagina() {
  const ultimaPagina = Math.ceil(this.totalItems / this.pageSize);
  this.paginator.pageIndex = ultimaPagina - 1;
  this.obtenerAdministradores(ultimaPagina);
}


  abrirCrear() {
    this.modo = 'crear';
    this.formularioEdit = {
      nombres: '',
      apellidos: '',
      email: '',
      telefono: '',
      contrasena: '',
      id: '',
    };
    this.mostrarPopup = true;
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

  cerrarPopup() {
    this.mostrarPopup = false;
    this.administradorId = null;
  }

  obtenerAdministradores(page: number) {
  
    
    this.cargando = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );

    let url = `https://api.bioemach.cl/administrador?page=${page}&pageSize=${this.pageSize}`;

    if (this.sortField) {
      url += `&sortField=${this.sortField}&sortDirection=${this.sortDirection}`;
    }

    this.http.get<any>(url, { headers }).subscribe({
      next: (response) => {
        this.administradores = response.data;
        this.totalItems = response.totalItems;
        this.pageSize = response.pageSize;
        this.currentPage = response.currentPage;
      },
      error: (error) => {
        console.error('Error al obtener administradores:', error);

        switch (error.status) {
          case 0:
            this.showErrorPopup('No se pudo conectar con el servidor.');
            break;
          case 401:
            this.tokenInvalido();
            break;
          case 500:
            this.showErrorPopup('Error interno del servidor.');
            break;
          default:
            this.showErrorPopup('Ocurrió un error inesperado.');
            break;
        }
      },
      complete: () => {
        this.cargando = false;
      },
    });
  }
  formatearFecha(fecha: moment.Moment): string {
    return fecha.format('YYYY-MM-DD');
  }


  /*--------------------*/
  /*      POPPUS        */
  /*--------------------*/

  abrirEdit(administradorId: number) {
    // Buscar el administrador en la lista actual
    //console.log('ADMINISTRADOR SELECIONADO :' + administradorId);
    this.modo = 'editar';

    const adminSeleccionado = this.administradores.find(
      (admin) => admin.administrador_id === administradorId
    );

    if (adminSeleccionado) {
      // Cargar los datos en el formulario de edición
      this.formularioEdit.nombres =
        adminSeleccionado.administrador_nombres || '';
      this.formularioEdit.apellidos =
        adminSeleccionado.administrador_apellidos || '';
      this.formularioEdit.email = adminSeleccionado.administrador_email || '';
      this.formularioEdit.telefono = adminSeleccionado.administrador_fono || '';
      this.formularioEdit.contrasena = ''; // por seguridad, dejar vacía
      this.formularioEdit.id = adminSeleccionado.administrador_id;

      this.administradorId = administradorId; // guardar id para editar

      // Mostrar el modal
      this.mostrarPopup = true;
    } else {
      this.showErrorPopup('No se encontró el administrador seleccionado.');
    }
  }

  cerrarPopupEdit() {
    this.mostrarPopup = false;
    this.administradorId = null;
    this.formularioEdit = {
      nombres: '',
      apellidos: '',
      email: '',
      telefono: '',
      contrasena: '',
      id: '',
    };
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


  ngAfterViewInit() {
    this.sort.sortChange.subscribe((sort) => {
      this.sortField = sort.active;
      this.sortDirection = sort.direction ? sort.direction : 'desc';
      this.paginator.pageIndex = 0;
      if (this.filtrosActivos) {
        this.aplicarFiltros(1);
      } else {
        this.obtenerAdministradores(1);
        
      }
    });

    this.paginator.page.subscribe((pageEvent: PageEvent) => {
      this.pageIndex = pageEvent.pageIndex;
      this.pageSize = pageEvent.pageSize;

      if (this.filtrosActivos) {
        this.aplicarFiltros(this.pageIndex + 1);
      } else {
        this.obtenerAdministradores(this.pageIndex + 1);
       
      }
    });

    if (this.filtrosActivos) {
      this.aplicarFiltros(1);
    } else {
     // this.obtenerAdministradores(1);   
    }
  }

  // Función para manejar el cambio de tamaño de página
  onPageSizeChange() {
    this.currentPage = 1; // resetear a primera página
    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerAdministradores(this.currentPage);
     
    }
  }
  onPageChange(event: PageEvent) {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;

    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerAdministradores(this.currentPage);
    
    }
  }

  tokenInvalido() {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }



  crearAdministrador() {
    if (!this.token) return;

    // Validar campos
    if (!this.formularioEdit.email || this.formularioEdit.email.trim() === '') {
      this.showErrorPopup('El campo Email no puede estar vacío');
      return;
    }

    if (
      !this.formularioEdit.contrasena ||
      this.formularioEdit.contrasena.trim() === ''
    ) {
      this.showErrorPopup(
        'La contraseña es obligatoria para crear un nuevo administrador'
      );
      return;
    }

    this.cargando = true;

    const headers = new HttpHeaders({
      Authorization: this.token,
      'Content-Type': 'application/json',
    });

    const body = {
      administrador_nombres: this.formularioEdit.nombres,
      administrador_apellidos: this.formularioEdit.apellidos,
      administrador_email: this.formularioEdit.email,
      administrador_fono: this.formularioEdit.telefono,
      administrador_password: this.formularioEdit.contrasena,
    };

    this.http
      .post<any>(
        `https://api.bioemach.cl/administrador/crear-administrador`,
        body,
        { headers }
      )
      .subscribe({
        next: (response) => {
          console.log('Administrador creado:', response);
          this.cargando = false;
          this.cerrarPopupEdit();
          this.obtenerAdministradores(1);
          this.showSuccessPopup('Administrador creado exitosamente');
        },
        error: (error) => {
          this.cargando = false;
          const mensaje = error.error?.mensaje || 'Ocurrió un error inesperado';

          switch (error.status) {
            case 400:
              this.showErrorPopup(
                'La contraseña debe tener al menos 8 dígitos, mayúsculas, minúsculas, un número y un carácter especial (# $ ! % & = ?)'
              ); // Ej: "Contraseña invalida, faltan caracteres"
              break;
            case 401:
              this.tokenInvalido();
              break;
            case 409:
              this.showErrorPopup(mensaje); // Ej: "El correo electrónico ya está registrado."
              break;
            case 500:
              this.showErrorPopup(
                'Error interno del servidor. Detalle: ' +
                  (error.error?.detalle || 'Sin detalles.')
              );
              break;
            default:
              this.showErrorPopup('Error desconocido. Intente nuevamente.');
              break;
          }
        },
      });
  }

  editarAdministrador() {
    this.cargando = true;

    const headers = new HttpHeaders().set(
      'Authorization',
      `Bearer ${this.token}`
    );
    const id = this.formularioEdit.id;

    const url = `https://api.bioemach.cl/administrador/edit/${id}`;

    const body = {
      administrador_nombres: this.formularioEdit.nombres,
      administrador_apellidos: this.formularioEdit.apellidos,
      administrador_email: this.formularioEdit.email,
      administrador_fono: this.formularioEdit.telefono,
      administrador_password: this.formularioEdit.contrasena,
    };

    this.http.put<any>(url, body, { headers }).subscribe({
      next: (response) => {
        this.cargando = false;

        if (response?.mensaje === 'Administrador actualizado exitosamente') {
          this.showSuccessPopup('Administrador actualizado correctamente.');
          this.mostrarPopup = false;
          this.obtenerAdministradores(this.currentPage);
        } else {
          this.showErrorPopup(response?.mensaje || 'Ocurrió un error.');
        }
      },
      error: (error) => {
        this.cargando = false;
        const mensaje =
          error.error?.mensaje || 'Error al actualizar administrador.';

        switch (error.status) {
          case 400:
            this.showErrorPopup(
              'La contraseña debe tener al menos 8 dígitos, mayúsculas, minúsculas, un número y un carácter especial (# $ ! % & = ?)'
            );
            break;

          case 401:
            this.tokenInvalido();
            break;

          case 404:
            this.showErrorPopup('Administrador no encontrado.');
            break;

          case 500:
            this.showErrorPopup(mensaje);
            break;

          default:
            this.showErrorPopup(mensaje);
        }

        console.error('Error al actualizar administrador:', error);
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
      params = params.set('nombres', this.nombre.trim());
    if (this.apellido.trim() !== '')
      params = params.set('apellidos', this.apellido.trim());
    if (this.email.trim() !== '')
      params = params.set('email', this.email.trim());
    if (this.telefono.trim() !== '')
      params = params.set('fono', this.telefono.trim());

    if (this.fechaInicio) {
      params = params.set('fechaInicio', this.formatearFecha(this.fechaInicio));
    }
    if (this.fechaFin) {
      params = params.set('fechaFin', this.formatearFecha(this.fechaFin));
    }

    this.http
      .get<any>('https://api.bioemach.cl/administrador/filtrar', {
        headers,
        params,
      })
      .subscribe({
        next: (response) => {
          this.administradores = response.data;
          this.totalItems = response.totalItems;
          this.pageSize = response.pageSize;
          this.currentPage = response.currentPage;
        },
        error: (error) => {
          this.cargando = false;

          if (error.status === 401) {
            this.tokenInvalido();
          } else if (error.status === 500) {
            const mensaje = error.error?.detalle || 'Error en el servidor';
            this.showErrorPopup(`Error al filtrar administradores: ${mensaje}`);
          } else {
            this.showErrorPopup(
              'Error desconocido al aplicar filtros. Contacte al área informática.'
            );
          }
        },
        complete: () => {
          this.cargando = false;
        },
      });
  }

  borrarAdministrador(administradorId: number): void {
    const dialogRef = this.dialog.open(InformationPopupComponent, {
      data: {
        message:
          '¿Está seguro que desea eliminar este administrador? Esta acción eliminará los registros asociados.\nEsta acción no se puede deshacer.',
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
          .delete<any>(
            `https://api.bioemach.cl/administrador/${administradorId}`,
            { headers }
          )
          .subscribe({
            next: (response) => {
              this.cargando = false;
              this.showSuccessPopup('Administrador eliminado exitosamente.');
              this.obtenerAdministradores(1);
            },
            error: (error) => {
              this.cargando = false;
              console.error('Error al borrar administrador:', error);

              const mensaje =
                error.error?.mensaje ??
                'Ocurrió un error al intentar eliminar el administrador.';

              switch (error.status) {
                case 401:
                  this.tokenInvalido();
                  break;

                case 404:
                  this.showErrorPopup(
                    'Administrador no encontrado. Verifique la información.'
                  );
                  break;

                case 500:
                  this.showErrorPopup(
                    'Error interno del servidor. Inténtelo más tarde.'
                  );
                  break;

                default:
                  this.showErrorPopup(mensaje);
                  break;
              }
            },
          });
      }
    });
  }

  exportar() {
    const sinFiltros =
      !this.nombre &&
      !this.apellido &&
      !this.email &&
      !this.telefono &&
      !this.fechaInicio &&
      !this.fechaFin;

    if (sinFiltros) {
      const dialogRef = this.dialog.open(InformationPopupComponent, {
        data: {
          message:
            'No se aplicaron filtros. ¿Desea exportar todos los Administradores?',
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

  // este método puede estar justo abajo o dentro del mismo componente
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
    const params = new HttpParams()
      .set('nombres', this.nombre || '')
      .set('apellidos', this.apellido || '')
      .set('email', this.email || '')
      .set('fono', this.telefono || '')
      .set('fechaInicio', formatDate(this.fechaInicio?.toDate() ?? null))
      .set('fechaFin', formatDate(this.fechaFin?.toDate() ?? null));

    const endpoint =
      'https://api.bioemach.cl/administrador/exportar/administradores';

    this.http
      .get(endpoint, {
        headers,
        params,
        responseType: 'blob',
        observe: 'response',
      })
      .subscribe({
        next: (response) => {
          const blob = response.body!;
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'administradores.xlsx';
          a.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err) => {
          switch (err.status) {
            case 401:
              this.tokenInvalido();

              break;
            case 500:
              this.showErrorPopup(
                'Error del servidor. Intente nuevamente más tarde.'
              );
              break;
            default:
              this.showErrorPopup(
                'Error al exportar Administradores.\nFavor ponerse en contacto con el área informática.'
              );
              break;
          }
          console.error('Error al exportar:', err);
        },
        complete: () => {
          this.cargando = false;
        },
      });
  }
}
