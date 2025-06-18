import { Component, OnInit, ViewChild } from '@angular/core';
import { MenuLateralComponent } from '../components/menu-lateral/menu-lateral.component';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { NgxPaginationModule } from 'ngx-pagination';
import { FormsModule, NgModel } from '@angular/forms'; // Importar FormsModule para usar ngModel
import { MatIconModule } from '@angular/material/icon';
import { ErrorPopupComponent } from '../components/error-popup/error-popup.component';
import { InformationPopupComponent } from '../components/information-popup/information-popup.component';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { DateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE, MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import {
  MatPaginator,
  MatPaginatorModule,
  PageEvent,
} from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { Router } from '@angular/router';
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
  selector: 'app-usuario',
  standalone: true,
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
  templateUrl: './usuario.component.html',
  styleUrl: './usuario.component.css',
  providers: [
    { provide: DateAdapter, useClass: MomentDateAdapter, deps: [MAT_DATE_LOCALE] },
    { provide: MAT_DATE_FORMATS, useValue: MY_DATE_FORMATS }
  ]
})
export class UsuarioComponent implements OnInit {
  menuVisible = true;
  mostrarPopup = false;

  token: string | null = null;
  sesiones: any[] = [];
  usuarios: any[] = [];
  sortField: string | null = null;
  sortDirection: string = 'desc';
  totalItems: number = 0;
  pageSize: number = 5; // Por defecto, 5 ítems por página
  currentPage: number = 1;
  pageIndex = 0;

  pagesArray: (number | string)[] = [];

  // Campos de filtro
  filtrosActivos: boolean = false;
  nombre: string = '';
  apellido: string = '';
  usuario: string = '';
  email: string = '';
  telefono: string = '';
  url: string = '';
  sistema: string = '';
fechaInicio: moment.Moment | null = null;
fechaFin: moment.Moment | null = null;

  campoOrden: string = '';
  ordenAscendente: boolean = true;

  mensajeSinSesiones: string | null = null;
  usuarioId: number | null = null;
  cargando: boolean = false;

  usuarioNombre: string = '';
  usuarioApellido: string = '';

   interaccionDentro = false;
  displayedColumns: string[] = [
    'usuario_nombres',
    'usuario_apellidos',
    'usuario_username',
    'usuario_email',
    'usuario_fono',
    'usuario_url',
    'created_at',
    'sistema_nombre',
    'acciones',
  ];
  modalDisplayedColumns: string[] = [
    'ip',
    'fechaCreacion',
    'fechaExpiracion',
    'diasRestantes',
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
      this.obtenerUsuarios(this.currentPage);
    }
  }


   limpiarFiltros() {
    this.nombre = '';
    this.apellido = '';
    this.email = '';
    this.telefono = '';
    this.fechaInicio = null;
    this.fechaFin = null;
    this.sistema='';
    this.usuario='';
    this.filtrosActivos = false;
    this.url='';

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
    this.obtenerUsuarios(1);
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

  aplicarFiltros(page: number = 1) {
  this.currentPage = page;
  this.filtrosActivos = true;
  this.cargando = true; // Activar spinner y bloqueo

  const headers = new HttpHeaders().set('Authorization', `Bearer ${this.token}`);

  let url = `https://api.bioemach.cl/usuario/filtrar?page=${this.currentPage}&pageSize=${this.pageSize}`;
  if (this.sortField) url += `&sortField=${this.sortField}&sortDirection=${this.sortDirection}`;
  if (this.nombre) url += `&nombres=${encodeURIComponent(this.nombre)}`;
  if (this.apellido) url += `&apellidos=${encodeURIComponent(this.apellido)}`;
  if (this.usuario) url += `&username=${encodeURIComponent(this.usuario)}`;
  if (this.email) url += `&email=${encodeURIComponent(this.email)}`;
  if (this.telefono) url += `&fono=${encodeURIComponent(this.telefono)}`;
  if (this.url) url += `&url=${encodeURIComponent(this.url)}`;
  if (this.sistema) url += `&sistema=${encodeURIComponent(this.sistema)}`;
  if (this.fechaInicio) url += `&fechaInicio=${encodeURIComponent(this.formatearFecha(this.fechaInicio))}`;
  if (this.fechaFin) url += `&fechaFin=${encodeURIComponent(this.formatearFecha(this.fechaFin))}`;

  this.http.get<any>(url, { headers, observe: 'response' }).subscribe({
    next: (response) => {
      switch (response.status) {
        case 200:
          this.usuarios = response.body.data;
          this.totalItems = response.body.totalItems;
          this.pageSize = response.body.pageSize;
          this.currentPage = response.body.currentPage;
          break;
        default:
          this.showErrorPopup('Respuesta desconocida del servidor');
          console.warn('Respuesta inesperada:', response);
          break;
      }
    },
    error: (err) => {
      switch (err.status) {
        case 401:
          this.tokenInvalido();
          break;
        case 500:
          this.showErrorPopup('Error interno del servidor\nPor favor, contacta al área informática.');
          break;
        default:
          this.showErrorPopup(`Error desconocido: ${err.statusText || 'Sin mensaje'}`);
          console.error('Error al filtrar:', err);
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

obtenerUsuarios(page: number) {
  this.cargando = true; // Mostrar spinner

  const headers = new HttpHeaders().set(
    'Authorization',
    `Bearer ${this.token}`
  );

  let url = `https://api.bioemach.cl/usuario?page=${page}&pageSize=${this.pageSize}`;
  if (this.sortField) {
    url += `&sortField=${this.sortField}&sortDirection=${this.sortDirection}`;
  }

  this.http.get<any>(url, { headers }).subscribe({
    next: (response) => {
      this.usuarios = response.data;
      this.totalItems = response.totalItems;
      this.pageSize = response.pageSize;
      this.currentPage = response.currentPage;
    },

    error: (error) => {
      switch (error.status) {
        case 0:
          this.showErrorPopup('No se pudo conectar con el servidor. Verifica tu conexión a internet.');
          break;
        case 401:
          this.tokenInvalido();
          break;
        case 403:
          this.showErrorPopup('No tienes permisos para ver esta información.');
          break;
        case 404:
          this.showErrorPopup('Recurso no encontrado. Contacta al área informática.');
          break;
        case 500:
          this.showErrorPopup('Error interno del servidor. Intenta más tarde o contacta soporte.');
          break;
        default:
          this.showErrorPopup('Error desconocido al obtener usuarios. Código: ' + error.status);
          break;
      }

      console.error('Error al obtener usuarios:', error);
    },

    complete: () => {
      this.cargando = false; // Ocultar spinner
    },
  });
}




  exportar() {
    
    const sinFiltros =
      !this.nombre &&
      !this.apellido &&
      !this.usuario &&
      !this.email &&
      !this.telefono &&
      !this.url &&
      !this.sistema &&
      !this.fechaInicio &&
      !this.fechaFin;

    if (sinFiltros) {
      const dialogRef = this.dialog.open(InformationPopupComponent, {
        data: {
          message:
            'No se aplicaron filtros. ¿Desea exportar todos los usuarios?',
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


private realizarExportacion(): void {
  this.cargando = true; // Activa spinner y bloquea botones

  const headers = new HttpHeaders().set('Authorization', `Bearer ${this.token}`);

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
    .set('username', this.usuario || '')
    .set('email', this.email || '')
    .set('fono', this.telefono || '')
    .set('url', this.url || '')
    .set('sistema', this.sistema || '')
    .set('fechaInicio', formatDate(this.fechaInicio?.toDate() ?? null))
   .set('fechaFin', formatDate(this.fechaFin?.toDate() ?? null))


  const endpoint = 'https://api.bioemach.cl/usuario/exportar/usuarios';

  this.http.get(endpoint, { headers, params, responseType: 'blob', observe: 'response' }).subscribe({
    next: (response) => {
      const blob = response.body!;
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'usuarios.xlsx';
      a.click();
      window.URL.revokeObjectURL(url);
    },
    error: async (err) => {
      // Caso especial: el error también puede venir en blob
      if (err.error instanceof Blob) {
        const errorText = await err.error.text();
        try {
          const jsonError = JSON.parse(errorText);
          switch (err.status) {
            case 401:
              this.tokenInvalido();
              break;
            case 500:
              this.showErrorPopup(`Error en el servidor: ${jsonError.detalle}`);
              break;
            default:
              this.showErrorPopup(`Error inesperado (${err.status}): ${jsonError.mensaje || 'Sin mensaje'}`);
              break;
          }
        } catch (e) {
          this.showErrorPopup(`Error inesperado (${err.status}). No se pudo interpretar la respuesta.`);
        }
      } else {
        this.showErrorPopup(`Error inesperado: ${err.message}`);
      }
    },
    complete: () => {
      this.cargando = false; // Oculta spinner
    }
  });
}


  cerrarTodasLasSesiones() {
    const dialogRef = this.dialog.open(InformationPopupComponent, {
      data: {
        message:
          'Esta acción cerrará todas las sesiones del usuario. ¿Está seguro?',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (confirmed) {
        this.ejecutarCierreSesiones();
      }
      // Si cancela, no hace nada
    });
  }

private ejecutarCierreSesiones() {
  if (this.usuarioId === null) return;

  this.cargando = true;

  const headers = new HttpHeaders().set(
    'Authorization',
    `Bearer ${this.token}`
  );
  const url = `https://api.bioemach.cl/administrador/cerrar_sesion_user?usuarioId=${this.usuarioId}`;

  this.http.post<any>(url, {}, { headers }).subscribe({
    next: (response) => {
      // Solo entra aquí si el status es 2xx
      console.log('Respuesta exitosa', response);
      this.sesiones = [];
      this.mensajeSinSesiones = 'No se encontraron sesiones activas para el usuario.';
      this.showSuccessPopup('Sesiones cerradas correctamente.');
    },
    error: (error) => {
      // Aquí entran los códigos 4xx y 5xx
      switch (error.status) {
        case 401:
          console.error('401 - No autorizado', error);
          this.tokenInvalido();
          break;

        case 404:
          console.warn('404 - No hay sesiones activas', error);
          this.sesiones = [];
          this.mensajeSinSesiones = 'No se encontraron sesiones activas.';
          break;

        case 500:
          console.error('500 - Error interno', error);
          this.showErrorPopup('Error interno del servidor. Intenta más tarde.');
          break;

        case 400:
          console.error('400 - Petición incorrecta', error);
          this.showErrorPopup('La petición no es válida.');
          break;

        default:
          console.error('Error inesperado', error);
          this.showErrorPopup('Error desconocido al cerrar las sesiones.');
          break;
      }
    },
    complete: () => {
      this.cargando = false;
    },
  });
}


cerrarSesionIP(ipUsuario: string) {
  if (this.usuarioId === null) return;

  this.cargando = true; // Activamos el spinner

  const headers = new HttpHeaders().set(
    'Authorization',
    `Bearer ${this.token}`
  );
  const url = `https://api.bioemach.cl/usuario/cerrar_sesion_ip?usuarioId=${this.usuarioId}&ipUsuario=${ipUsuario}`;

  this.http.delete(url, { headers }).subscribe({
    next: (response: any) => {
      console.log('Sesión cerrada exitosamente', response);

      // Eliminamos la sesión de la lista
      this.sesiones = this.sesiones.filter((s) => s.ip !== ipUsuario);

      if (this.sesiones.length === 0) {
        this.mensajeSinSesiones = 'No se encontraron sesiones activas para el usuario.';
      }

      this.showSuccessPopup(response?.mensaje || 'Sesión cerrada correctamente.');
    },

    error: (error) => {
      console.error('Error al cerrar sesión:', error);

      switch (error.status) {
        case 401:
          this.tokenInvalido();
          break;

        case 404:
          this.showErrorPopup('No se encontró una sesión activa para la IP especificada.');
          break;

        case 500:
          this.showErrorPopup('Error interno del servidor. Inténtelo nuevamente más tarde.');
          break;

        case 0:
          this.showErrorPopup('No se pudo conectar al servidor. Verifica tu conexión.');
          break;

        default:
          this.showErrorPopup('Error desconocido al cerrar la sesión.');
          break;
      }
    },

    complete: () => {
      this.cargando = false; // Ocultamos el spinner al finalizar
    },
  });
}


obtenerTokensUsuario(usuarioId: number) {
  const headers = new HttpHeaders().set('Authorization', `${this.token}`);
  const url = `https://api.bioemach.cl/usuario/obtenerTokens/${usuarioId}`;

  this.http.get<any>(url, { headers }).subscribe({
    next: (response) => {
      // Manejo de éxito (200)
      if (response.data && Array.isArray(response.data) && response.data.length > 0) {
        this.sesiones = response.data.map((item: any) => ({
          ip: item.token_ip,
          fechaCreacion: item.created_at,
          fechaExpiracion: item.token_expires_at,
          diasRestantes: item.dias_restantes,
        }));
        this.mensajeSinSesiones = null;
      } else {
        // Caso raro si llega 200 sin datos
        this.sesiones = [];
        this.mensajeSinSesiones = 'No se encontraron sesiones activas para el usuario.';
      }
    },
    error: (error) => {
      this.sesiones = []; // Limpiar datos por seguridad

      switch (error.status) {
        case 401:
          this.tokenInvalido();
          break;

        case 404:
          this.mensajeSinSesiones = 'No se encontraron sesiones activas para el usuario.';
          break;

        case 500:
          this.mensajeSinSesiones = 'Error interno del servidor. Inténtelo nuevamente más tarde.';
          break;

        default:
          this.mensajeSinSesiones = 'Error desconocido al obtener sesiones. Contacte al área informática.';
          break;
      }

      console.error('Error al obtener sesiones del usuario:', error);
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
        this.obtenerUsuarios(1);
      }
    });

    this.paginator.page.subscribe((pageEvent: PageEvent) => {
      this.pageIndex = pageEvent.pageIndex;
      this.pageSize = pageEvent.pageSize;

      if (this.filtrosActivos) {
        this.aplicarFiltros(this.pageIndex + 1);
      } else {
        this.obtenerUsuarios(this.pageIndex + 1);
      }
    });

    if (this.filtrosActivos) {
      this.aplicarFiltros(1);
    } else {
      //this.obtenerUsuarios(1);
    }
  }

  onPageChange(event: PageEvent) {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;

    if (this.filtrosActivos) {
      this.aplicarFiltros(this.currentPage);
    } else {
      this.obtenerUsuarios(this.currentPage);
    }
  }

  abrirPopup(usuarioId: number) {
    console.log('Usuario ID: ', usuarioId);
    this.usuarioId = usuarioId;
    this.mostrarPopup = true;

    // Busca el usuario dentro del arreglo usuarios para obtener nombre y apellido
    const usuario = this.usuarios.find((u) => u.usuario_id === usuarioId);
    if (usuario) {
      this.usuarioNombre = usuario.usuario_nombres;
      this.usuarioApellido = usuario.usuario_apellidos;
    } else {
      this.usuarioNombre = '';
      this.usuarioApellido = '';
      console.error('No se encontró usuario para mostrar nombre');
    }

    if (this.usuarioId !== null) {
      this.obtenerTokensUsuario(this.usuarioId);
    } else {
      console.error('usuarioId es null');
    }
  }

  cerrarPopup() {
    this.mostrarPopup = false;
    this.usuarioId = null; // Limpiamos el ID cuando se cierra el popup
  }
  showErrorPopup(message: string): void {
    this.dialog.open(ErrorPopupComponent, {
      data: { message },
    });
  }

   showSuccessPopup(message: string): void {
    this.dialog.open(ErrorPopupComponent, {
      data: { message },
    });
  }

  onMenuToggle(state: boolean) {
    this.menuVisible = state;
  }

 


    tokenInvalido() {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
