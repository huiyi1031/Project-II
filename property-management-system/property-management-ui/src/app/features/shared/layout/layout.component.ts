import { Component, Input, OnInit, OnDestroy, HostListener } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

export interface MenuItem {
  label: string;
  route?: string;        // leaf route (relative child route)
  children?: MenuItem[];  // grouped sub-items → renders as dropdown
}

/* ── Menu definitions per role ────────────────────────────────────────── */
const MENUS: Record<string, MenuItem[]> = {
  Occupant: [
    { label: 'Dashboard',           route: 'dashboard' },
    { label: 'New Request',         route: 'create-request' },
    { label: 'Track Request',       route: 'track-request' },
    { label: 'Chat',                route: 'chat' },
    { label: 'My Property',         route: 'my-property' },
  ],
  Technician: [
    { label: 'Dashboard',           route: 'dashboard' },
    { label: 'Work Orders',         route: 'work-orders' },
    { label: 'Execute Work',        route: 'execute-work' },
    { label: 'Chat',                route: 'chat' },
    { label: 'Report',              route: 'report' },
  ],
  PropertyManager: [
    { label: 'Dashboard',           route: 'dashboard' },
    { label: 'Account Management',  children: [
      { label: 'Staff Accounts',    route: 'staff' },
      { label: 'Owner / Tenant',    route: 'occupants' },
    ]},
    { label: 'Requests',            route: 'requests' },
    { label: 'Work Orders',         route: 'work-orders' },
    { label: 'Property Units',      route: 'units' },
    { label: 'Assets',              route: 'assets' },
    { label: 'Proactive',           route: 'proactive' },
    { label: 'Chat',                route: 'chat' },
  ],
};

@Component({
  selector: 'app-layout',
  templateUrl: './layout.component.html',
  standalone: false,
})
export class LayoutComponent implements OnInit, OnDestroy {
  @Input() rolePrefix = '';   // 'tenant' | 'technician' | 'manager'

  menuItems:   MenuItem[] = [];
  activeItem   = 'dashboard';
  roleLabel    = '';
  occupantType = '';
  userName     = '';
  userEmail    = '';

  /* UI state */
  openDropdown: string | null = null;   // label of currently open dropdown
  isMobileMenuOpen = false;
  isProfileMenuOpen = false;

  private sub!: Subscription;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.roleLabel    = user.role;
      this.occupantType = (user as any).occupantType || '';
      this.userName     = user.fullName || user.email;
      this.userEmail    = user.email;
      this.menuItems    = [...(MENUS[user.role] ?? [])];
    }

    this.sub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e: any) => this.syncActive(e.urlAfterRedirects));

    this.syncActive(this.router.url);
  }

  ngOnDestroy(): void { this.sub?.unsubscribe(); }

  /* ── Navigation ──────────────────────────────────────────────────────── */
  navigate(item: MenuItem): void {
    if (!item.route) return;
    const base = `/${this.rolePrefix}`;
    this.router.navigate([base, item.route]);
    this.activeItem = item.route;
    this.openDropdown = null;
    this.isMobileMenuOpen = false;
  }

  /* ── Dropdown toggle ─────────────────────────────────────────────────── */
  toggleDropdown(label: string, event: Event): void {
    event.stopPropagation();
    this.openDropdown = this.openDropdown === label ? null : label;
  }

  /* ── Mobile hamburger ────────────────────────────────────────────────── */
  toggleMobile(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    if (!this.isMobileMenuOpen) this.openDropdown = null;
  }

  /* ── Profile dropdown ────────────────────────────────────────────────── */
  toggleProfileMenu(event: Event): void {
    event.stopPropagation();
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
  }

  /* ── Click-outside to close dropdowns ────────────────────────────────── */
  @HostListener('document:click')
  onDocumentClick(): void {
    this.openDropdown = null;
    this.isProfileMenuOpen = false;
  }

  logout(): void { this.authService.logout(); }

  /* ── Helpers ─────────────────────────────────────────────────────────── */
  isActive(item: MenuItem): boolean {
    if (item.route) return this.activeItem === item.route;
    // Group is active if any child is active
    return !!item.children?.some(c => c.route === this.activeItem);
  }

  getInitial(): string {
    return this.userName ? this.userName.charAt(0).toUpperCase() : '?';
  }

  private syncActive(url: string): void {
    const seg = url.split('/').pop() ?? 'dashboard';
    this.activeItem = seg;
  }
}
