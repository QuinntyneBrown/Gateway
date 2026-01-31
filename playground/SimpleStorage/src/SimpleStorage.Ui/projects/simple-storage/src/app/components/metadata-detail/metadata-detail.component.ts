import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MetadataService, Metadata, CreateMetadataRequest, UpdateMetadataRequest } from '../../services/metadata.service';

@Component({
  selector: 'app-metadata-detail',
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './metadata-detail.component.html',
  styleUrls: ['./metadata-detail.component.scss']
})
export class MetadataDetailComponent implements OnInit {
  private metadataService = inject(MetadataService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  metadataForm: FormGroup;
  loading = signal(false);
  isEditMode = signal(false);
  isCreateMode = signal(false);
  metadataId = signal<string | null>(null);
  metadata = signal<Metadata | null>(null);

  constructor() {
    this.metadataForm = this.fb.group({
      fileName: ['', Validators.required],
      contentType: ['', Validators.required],
      fileSize: [0, [Validators.required, Validators.min(0)]],
      fileType: [0, Validators.required],
      storagePath: ['', Validators.required],
      version: ['', Validators.required],
      uploadedBy: ['', Validators.required]
    });
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    const isEdit = this.route.snapshot.url.some(segment => segment.path === 'edit');
    const isCreate = this.route.snapshot.url.some(segment => segment.path === 'create');

    this.isCreateMode.set(isCreate);
    
    if (isCreate) {
      this.metadataForm.enable();
    } else if (id) {
      this.metadataId.set(id);
      this.isEditMode.set(isEdit);
      this.loadMetadata(id);
      
      if (!isEdit) {
        this.metadataForm.disable();
      }
    }
  }

  loadMetadata(id: string) {
    this.loading.set(true);
    this.metadataService.getById(id).subscribe({
      next: (data: Metadata) => {
        this.metadata.set(data);
        this.metadataForm.patchValue({
          fileName: data.fileName,
          contentType: data.contentType,
          fileSize: data.fileSize,
          fileType: data.fileType,
          storagePath: data.storagePath,
          version: data.version,
          uploadedBy: data.uploadedBy
        });
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading metadata:', error);
        this.loading.set(false);
      }
    });
  }

  onSave() {
    if (this.metadataForm.invalid) {
      return;
    }

    this.loading.set(true);

    if (this.isCreateMode()) {
      const request: CreateMetadataRequest = this.metadataForm.value;
      this.metadataService.create(request).subscribe({
        next: (data) => {
          this.router.navigate(['/metadata', data.id]);
        },
        error: (error) => {
          console.error('Error creating metadata:', error);
          this.loading.set(false);
        }
      });
    } else if (this.metadataId()) {
      const request: UpdateMetadataRequest = {
        fileName: this.metadataForm.value.fileName,
        contentType: this.metadataForm.value.contentType,
        fileSize: this.metadataForm.value.fileSize,
        fileType: this.metadataForm.value.fileType,
        storagePath: this.metadataForm.value.storagePath,
        version: this.metadataForm.value.version
      };
      this.metadataService.update(this.metadataId()!, request).subscribe({
        next: (data) => {
          this.router.navigate(['/metadata', data.id]);
        },
        error: (error) => {
          console.error('Error updating metadata:', error);
          this.loading.set(false);
        }
      });
    }
  }

  onCancel() {
    if (this.isCreateMode()) {
      this.router.navigate(['/metadata']);
    } else {
      this.router.navigate(['/metadata', this.metadataId()]);
    }
  }

  onDelete() {
    if (this.metadataId() && confirm('Are you sure you want to delete this metadata?')) {
      this.metadataService.delete(this.metadataId()!).subscribe({
        next: () => {
          this.router.navigate(['/metadata']);
        },
        error: (error) => {
          console.error('Error deleting metadata:', error);
        }
      });
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  formatDate(date: Date | string | undefined): string {
    if (!date) return 'N/A';
    const d = new Date(date);
    return d.toLocaleString();
  }
}
