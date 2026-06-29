import { NgModule } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';

import { LayoutComponent } from './layout/layout.component';
import { StatusBadgeComponent } from './components/status-badge/status-badge.component';
import { ChatComponent } from '../tenant/chat/chat.component';
import { ProfileComponent } from '../tenant/profile/profile.component';
import { IdPrefixPipe } from './pipes/id-prefix.pipe';

@NgModule({
  declarations: [
    LayoutComponent,
    StatusBadgeComponent,
    ChatComponent,
    ProfileComponent,
    IdPrefixPipe,
  ],
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    FormsModule,
  ],
  providers: [DatePipe],
  exports: [
    // Layout
    LayoutComponent,
    // Components
    StatusBadgeComponent,
    ChatComponent,
    ProfileComponent,
    IdPrefixPipe,
    // Re-export common modules for feature modules
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule,
  ],
})
export class SharedModule {}
