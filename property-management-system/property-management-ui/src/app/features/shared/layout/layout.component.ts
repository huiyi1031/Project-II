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
    { label: 'Dashboard', route: 'dashboard' },
    {
      label: 'Property', children: [
        { label: 'My Property', route: 'my-property' },
      ]
    },
    {
      label: 'Maintenance', children: [
        { label: 'New Request', route: 'create-request' },
        { label: 'Track Request', route: 'track-request' },
        { label: 'Chat', route: 'chat' },
      ]
    },
  ],
  Technician: [
    { label: 'Dashboard', route: 'dashboard' },
    {
      label: 'Maintenance', children: [
        { label: 'Work Orders', route: 'work-orders' },
        { label: 'Execute Work', route: 'execute-work' },
        { label: 'Report', route: 'report' },
        { label: 'Chat', route: 'chat' },
      ]
    },
  ],
  PropertyManager: [
    { label: 'Dashboard', route: 'dashboard' },
    {
      label: 'Property', children: [
        { label: 'Property Units', route: 'units' },
        { label: 'Assets', route: 'assets' },
      ]
    },
    {
      label: 'Maintenance', children: [
        { label: 'Maintenance Requests', route: 'requests' },
        { label: 'Work Orders', route: 'work-orders' },
        { label: 'Proactive Maintenance', route: 'proactive' },
        { label: 'Chat', route: 'chat' },
      ]
    },
    {
      label: 'Account', children: [
        { label: 'Owner / Tenant', route: 'occupants' },
        { label: 'Staff Accounts', route: 'staff' },
      ]
    },
  ],
};

@Component({
  selector: 'app-layout',
  templateUrl: './layout.component.html',
  standalone: false,
})
export class LayoutComponent implements OnInit, OnDestroy {
  @Input() rolePrefix = '';   // 'tenant' | 'technician' | 'manager'

  menuItems: MenuItem[] = [];
  activeItem = 'dashboard';
  roleLabel = '';
  occupantType = '';
  userName = '';
  userEmail = '';

  /* UI state */
  openGroups: Record<string, boolean> = {
    'Property': true,
    'Maintenance': true,
    'Account': true,
  };
  isSidebarOpen = false;
  isProfileMenuOpen = false;

  private sub!: Subscription;

  constructor(private authService: AuthService, private router: Router) { }

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.roleLabel = user.role;
      this.occupantType = (user as any).occupantType || '';
      this.userName = user.fullName || user.email;
      this.userEmail = user.email;
      this.menuItems = [...(MENUS[user.role] ?? [])];
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
    if (window.innerWidth <= 1024) {
      this.isSidebarOpen = false;
    }
  }

  /* ── Hover controls ──────────────────────────────────────────────────── */
  onSidebarMouseEnter(): void {
    if (window.innerWidth > 1024) {
      this.isSidebarOpen = true;
    }
  }

  onSidebarMouseLeave(): void {
    if (window.innerWidth > 1024) {
      this.isSidebarOpen = false;
    }
  }

  onGroupHover(label: string): void {
    if (this.isSidebarOpen) {
      this.openGroups[label] = true;
    }
  }

  /* ── Dropdown toggle ─────────────────────────────────────────────────── */
  toggleDropdown(label: string, event: Event): void {
    event.stopPropagation();
    if (!this.isSidebarOpen) {
      this.isSidebarOpen = true;
      this.openGroups[label] = true;
      return;
    }
    this.openGroups[label] = !this.openGroups[label];
  }

  /* ── Sidebar toggle ────────────────────────────────────────────────── */
  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  /* ── Profile dropdown ────────────────────────────────────────────────── */
  toggleProfileMenu(event: Event): void {
    event.stopPropagation();
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
  }

  /* ── Click-outside to close dropdowns ────────────────────────────────── */
  @HostListener('document:click')
  onDocumentClick(): void {
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
