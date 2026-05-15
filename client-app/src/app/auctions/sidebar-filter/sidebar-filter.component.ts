import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

export interface SidebarFilterValue {
  categories: string[];
  minPrice: number | null;
  maxPrice: number | null;
}

@Component({
  selector: 'app-sidebar-filter',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sidebar-filter.component.html',
  styleUrl: './sidebar-filter.component.scss'
})
export class SidebarFilterComponent {
  @Input() categories: string[] = [];
  @Output() readonly filterChanged = new EventEmitter<SidebarFilterValue>();

  selectedCategories = new Set<string>();
  minPrice: number | null = null;
  maxPrice: number | null = null;

  onCategoryChange(category: string, checked: boolean): void {
    if (checked) {
      this.selectedCategories.add(category);
    } else {
      this.selectedCategories.delete(category);
    }

    this.emitValue();
  }

  onPriceChange(): void {
    this.emitValue();
  }

  clearAll(): void {
    this.selectedCategories.clear();
    this.minPrice = null;
    this.maxPrice = null;
    this.emitValue();
  }

  isSelected(category: string): boolean {
    return this.selectedCategories.has(category);
  }

  private emitValue(): void {
    this.filterChanged.emit({
      categories: [...this.selectedCategories],
      minPrice: this.minPrice,
      maxPrice: this.maxPrice
    });
  }
}
