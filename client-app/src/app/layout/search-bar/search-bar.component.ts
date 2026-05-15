import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  templateUrl: 'search-bar.component.html',
  styleUrls: ['search-bar.component.scss'],
  imports: [CommonModule],
})
export class SearchBarComponent {
  @Output() readonly searchChanged = new EventEmitter<string>();

  query = '';

  onInput(value: string): void {
    this.query = value;
    this.searchChanged.emit(value.trim());
  }

  clear(): void {
    this.query = '';
    this.searchChanged.emit('');
  }
}