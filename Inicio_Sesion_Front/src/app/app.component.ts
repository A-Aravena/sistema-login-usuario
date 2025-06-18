import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-root',
  imports: [RouterModule],
  template: `
    <router-outlet></router-outlet>
  `
})
export class AppComponent {  constructor(private router: Router) {
    if (window.location.pathname === '/') {
      this.router.navigate(['/login']);
    }
  }}