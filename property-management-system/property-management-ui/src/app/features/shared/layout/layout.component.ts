import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

export interface MenuItem {
  label: string;
  icon:  string;
  route: string;       // relative child route
}

const MENUS: Record<string, MenuItem[]> = {
  Occupant: [
    { label: 'Dashboard',                  icon: '🏠', route: 'dashboard' },
    { label: 'Create Maintenance Request', icon: '📝', route: 'create-request' },
    { label: 'Track Request',              icon: '🔍', route: 'track-request' },
    { label: 'Maintenance Chat Room',      icon: '💬', route: 'chat' },
    { label: 'My Property',                icon: '🏢', route: 'my-property' },
  ],
  Technician: [
    { label: 'Dashboard',        icon: '🏠', route: 'dashboard' },
    { label: 'View Work Order',  icon: '📋', route: 'work-orders' },
    { label: 'Execute Work Order',icon: '🔧', route: 'execute-work' },
    { label: 'Chat Room',        icon: '💬', route: 'chat' },
    { label: 'Report',           icon: '📊', route: 'report' },
  ],
  PropertyManager: [
    { label: 'Dashboard',                   icon: '🏠', route: 'dashboard' },
    { label: 'Staff Account Management',    icon: '👥', route: 'staff' },
    { label: 'Tenant/Owner Management',     icon: '🧑‍🤝‍🧑', route: 'occupants' },
    { label: 'Maintenance Request Mgmt',    icon: '📝', route: 'requests' },
    { label: 'Work Order Management',       icon: '🔧', route: 'work-orders' },
    { label: 'Property Unit Management',    icon: '🏢', route: 'units' },
    { label: 'Asset Management',            icon: '⚙️', route: 'assets' },
    { label: 'Proactive Maintenance',       icon: '🛡️', route: 'proactive' },
    { label: 'Maintenance Chat Room',       icon: '💬', route: 'chat' },
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
  occupantType = '';   // 'Owner' | 'Tenant' | 'Resident'
  pageTitle    = 'Dashboard';
  breadcrumb   = '';
  userName     = '';
  userEmail    = '';
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

      // Start with the base menu for the role
      const base = [...(MENUS[user.role] ?? [])];

      this.menuItems = base;
    }

    // Track active route for highlight
    this.sub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e: any) => this.syncActive(e.urlAfterRedirects));

    this.syncActive(this.router.url);
  }

  ngOnDestroy(): void { this.sub?.unsubscribe(); }

  navigate(item: MenuItem): void {
    const base = `/${this.rolePrefix}`;
    this.router.navigate([base, item.route]);
    this.pageTitle  = item.label;
    this.breadcrumb = `${this.roleLabel} › ${item.label}`;
    this.activeItem = item.route;
  }

  logout(): void { this.authService.logout(); }

  toggleProfileMenu(): void {
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
  }

  private syncActive(url: string): void {
    const seg = url.split('/').pop() ?? 'dashboard';
    this.activeItem = seg;
    const found = this.menuItems.find(m => m.route === seg);
    if (found) {
      this.pageTitle  = found.label;
      this.breadcrumb = `${this.roleLabel} › ${found.label}`;
    }
  }
}
