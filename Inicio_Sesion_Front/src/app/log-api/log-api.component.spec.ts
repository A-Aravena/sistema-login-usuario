import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LogApiComponent } from './log-api.component';

describe('LogApiComponent', () => {
  let component: LogApiComponent;
  let fixture: ComponentFixture<LogApiComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LogApiComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LogApiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
