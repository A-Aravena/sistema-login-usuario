// information-popup.component.ts
import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-information-popup',
  standalone: true,
  imports: [CommonModule, MatDialogModule,MatIconModule],
  styleUrls: ['./information-popup.component.css'],
template: `
  <div class="popup-container">
    <div class="popup-header">

      <span class="popup-title">Información</span>
    </div>
    <div class="popup-content">
      <p>{{ data.message }}</p>
    </div>
    <div class="popup-actions">
      <button mat-button (click)="onCancel()" color="warn">Cancelar</button>
      <button mat-button (click)="onConfirm()" color="primary">Aceptar</button>
    </div>
  </div>
`
})
export class InformationPopupComponent {
  constructor(
    public dialogRef: MatDialogRef<InformationPopupComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { message: string }
  ) {}

  onConfirm(): void {
    this.dialogRef.close(true); // equivalente a que el usuario aceptó
  }

  onCancel(): void {
    this.dialogRef.close(false); // equivalente a que el usuario canceló
  }
}