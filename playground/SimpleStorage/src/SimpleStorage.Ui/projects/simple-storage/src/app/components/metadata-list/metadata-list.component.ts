import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormsModule } from '@angular/forms';
import { MetadataService, Metadata, PagedResult } from '../../services/metadata.service';

@Component({
  selector: 'app-metadata-list',
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatTooltipModule,
    FormsModule
  ],
  templateUrl: './metadata-list.component.html',
  styleUrls: ['./metadata-list.component.scss']
})
export class MetadataListComponent implements OnInit {
  private metadataService = inject(MetadataService);
  private dialog = inject(MatDialog);

  displayedColumns = ['fileName', 'contentType', 'fileSize', 'fileType', 'uploadedAt', 'uploadedBy', 'actions'];
  
  dataSource = signal<Metadata[]>([]);
  loading = signal(false);
  pageNumber = signal(1);
  pageSize = signal(10);
  hasPreviousPage = signal(false);
  hasNextPage = signal(false);
  fileTypeFilter = signal<number | undefined>(undefined);

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading.set(true);
    this.metadataService.getPage(this.pageNumber(), this.pageSize(), this.fileTypeFilter()).subscribe({
      next: (result: PagedResult) => {
        this.dataSource.set(result.items);
        this.hasPreviousPage.set(result.hasPreviousPage);
        this.hasNextPage.set(result.hasNextPage);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading metadata:', error);
        this.loading.set(false);
      }
    });
  }

  onFilterChange() {
    this.pageNumber.set(1);
    this.loadData();
  }

  previousPage() {
    if (this.hasPreviousPage()) {
      this.pageNumber.update(p => p - 1);
      this.loadData();
    }
  }

  nextPage() {
    if (this.hasNextPage()) {
      this.pageNumber.update(p => p + 1);
      this.loadData();
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  formatDate(date: Date | string): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleString();
  }

  onDelete(id: string) {
    if (confirm('Are you sure you want to delete this metadata?')) {
      this.metadataService.delete(id).subscribe({
        next: () => {
          this.loadData();
        },
        error: (error) => {
          console.error('Error deleting metadata:', error);
        }
      });
    }
  }
}
