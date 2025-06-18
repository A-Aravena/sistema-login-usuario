import { Route } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { OlvidarContrasenaComponent } from './olvidar-contrasena/olvidar-contrasena.component';
import {  ValidarCodigoComponent} from './validar-codigo/validar-codigo.component';
import {  UsuarioComponent} from './usuario/usuario.component';
import {  RestaurarContrasenaComponent} from './restaurar-contrasena/restaurar-contrasena.component';
import { AdministradorComponent } from './administrador/administrador.component';
import { SistemaComponent } from './sistema/sistema.component';
import { LogApiComponent } from './log-api/log-api.component';
import { LogSistemaComponent } from './log-sistema/log-sistema.component';
import { ConfiguracionComponent } from './configuracion/configuracion.component';


export const routes: Route[] = [
  { path: 'login', component: LoginComponent },
  { path: 'olvidar-contrasena', component: OlvidarContrasenaComponent },
  { path: 'validar-codigo', component: ValidarCodigoComponent },
  { path: 'restaurar-contrasena', component: RestaurarContrasenaComponent },

  {
    path: 'usuario',
    component: UsuarioComponent
  }
  ,
  {
    path: 'administrador',
    component: AdministradorComponent
  }
  ,
  {
    path: 'sistema',
    component: SistemaComponent
  }
  ,
  {
    path: 'log_api',
    component: LogApiComponent
  }
  ,
  {
    path: 'log_sistema',
    component: LogSistemaComponent
  }
  ,
  {
    path: 'configuracion',
    component: ConfiguracionComponent
  }
];