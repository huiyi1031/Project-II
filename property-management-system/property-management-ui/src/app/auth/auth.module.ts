import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LoginComponent } from './login/login.component';

@NgModule({
  declarations: [
    LoginComponent  // ← Declare the LoginComponent so Angular knows about it
  ],
  imports: [
    CommonModule,         // ← Provides *ngIf, *ngFor, etc.
    ReactiveFormsModule,  // ← Provides formGroup, formControl, etc.
    RouterModule.forChild([
      // Routes specific to the auth module
      { path: 'login', component: LoginComponent },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ])
  ]
})
export class AuthModule { }