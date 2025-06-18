import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';

import { SuccessPopupComponent } from '../success-popup/success-popup.component';
import { InformationPopupComponent } from '../information-popup/information-popup.component';
import { ErrorPopupComponent } from '../error-popup/error-popup.component';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs';

@Component({
  selector: 'app-menu-lateral',
  templateUrl: './menu-lateral.component.html',
  styleUrls: ['./menu-lateral.component.css'],
  standalone: true,
  imports: [
      CommonModule,
  RouterModule,
  MatSidenavModule,
  MatIconModule,
  MatButtonModule,
  MatListModule 
  ],
})
export class MenuLateralComponent implements OnInit {
  isMenuVisible = true;
  isDropdownOpen = false;

  @Output() menuToggle = new EventEmitter<boolean>();

  constructor(
    private dialog: MatDialog,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    // Escucha cambios de ruta y decide si abrir el submenú
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.isDropdownOpen = this.shouldDropdownBeOpen(event.urlAfterRedirects);
      });

    // Forzar apertura al iniciar si ya estamos en una ruta hija
    this.isDropdownOpen = this.shouldDropdownBeOpen(this.router.url);
  }

  shouldDropdownBeOpen(url: string): boolean {
    return url.includes('/log_api') || url.includes('/log_sistema');
  }

  toggleMenu() {
    this.isMenuVisible = !this.isMenuVisible;
    this.menuToggle.emit(this.isMenuVisible);
  }

  toggleDropdown() {
    this.isDropdownOpen = !this.isDropdownOpen;
  }


  logout() {
    const dialogRef = this.dialog.open(InformationPopupComponent, {
      data: {
        message: '¿Desea cerrar sesión?',
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.realizarLogout();
      }
    });
  }

  realizarLogout() {
    const token = localStorage.getItem('token'); // O como lo guardes

    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http
      .post('https://api.bioemach.cl/administrador/cerrar-sesion', null, {
        headers,
      })
      .subscribe({
        next: (res: any) => {
          // Si la API responde correctamente
          console.log('Sesión cerrada correctamente:', res);
          localStorage.removeItem('token'); //
          this.router.navigateByUrl('/login', { replaceUrl: true }).then(() => {
            window.history.pushState(null, '', window.location.href); // resetea el historial
          });
        },
        error: (err) => {
          console.error('Error al cerrar sesión:', err);
          this.dialog.open(ErrorPopupComponent, {
            data: {
              message:
                err?.error?.mensaje ||
                'Ocurrió un error al cerrar sesión. Inténtelo nuevamente.',
            },
          });
        },
      });
  }
}
