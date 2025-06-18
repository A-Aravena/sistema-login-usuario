import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LogSistemaComponent } from './log-sistema.component';

describe('LogSistemaComponent', () => {
  let component: LogSistemaComponent;
  let fixture: ComponentFixture<LogSistemaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LogSistemaComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LogSistemaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
