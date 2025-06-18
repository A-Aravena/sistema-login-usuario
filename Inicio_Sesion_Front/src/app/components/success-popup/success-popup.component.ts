import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-success-popup',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatIconModule],
  styleUrls: ['./success-popup.component.css'],
  template: `
    <div class="popup-container">
      <div class="popup-header">
        
        <span class="popup-title">Mensaje Éxito</span>
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
export class SuccessPopupComponent {
  constructor(
    public dialogRef: MatDialogRef<SuccessPopupComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { message: string }
  ) {}

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}