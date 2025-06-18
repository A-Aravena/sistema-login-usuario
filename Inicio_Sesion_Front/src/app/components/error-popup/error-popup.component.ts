import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-error-popup',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatIconModule],
  styleUrls: ['./error-popup.component.css'],
  template: `
    <div class="popup-container">
      <div class="popup-header">
     
        <span class="popup-title">Mensaje Error</span>
      </div>
      <div class="popup-content">
        <p>{{ data.message }}</p>
      </div>
      <div class="popup-actions">
        <button mat-button (click)="onConfirm()" color="primary">Aceptar</button>
      </div>
    </div>
  `
})
export class ErrorPopupComponent {
  constructor(
    public dialogRef: MatDialogRef<ErrorPopupComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { message: string }
  ) {}

  onConfirm(): void {
    this.dialogRef.close(true); // Cierra el popup con resultado true
  }
}