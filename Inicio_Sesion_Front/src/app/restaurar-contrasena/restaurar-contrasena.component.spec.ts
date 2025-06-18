import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RestaurarContrasenaComponent } from './restaurar-contrasena.component';

describe('RestaurarContrasenaComponent', () => {
  let component: RestaurarContrasenaComponent;
  let fixture: ComponentFixture<RestaurarContrasenaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RestaurarContrasenaComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RestaurarContrasenaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
