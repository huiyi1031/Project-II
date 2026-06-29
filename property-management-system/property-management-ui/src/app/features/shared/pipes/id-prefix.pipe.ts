import { Pipe, PipeTransform } from '@angular/core';

/** Formats a number as a padded ID string, e.g.  12 → 'REQ-2026-0012' */
@Pipe({ name: 'idPrefix', standalone: false })
export class IdPrefixPipe implements PipeTransform {
  transform(value: number | string, prefix: string = 'REQ'): string {
    const num = String(value).padStart(4, '0');
    return `${prefix}-2026-${num}`;
  }
}
