import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Metadata {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  fileType: number;
  storagePath: string;
  version: string;
  uploadedAt: Date;
  modifiedAt?: Date;
  uploadedBy: string;
  tags?: Record<string, any>;
}

export interface CreateMetadataRequest {
  fileName: string;
  contentType: string;
  fileSize: number;
  fileType: number;
  storagePath: string;
  version: string;
  uploadedBy: string;
  tags?: Record<string, any>;
}

export interface UpdateMetadataRequest {
  fileName: string;
  contentType: string;
  fileSize: number;
  fileType: number;
  storagePath: string;
  version: string;
  tags?: Record<string, any>;
}

export interface PagedResult {
  items: Metadata[];
  pageNumber: number;
  pageSize: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  filter?: { fileType?: number };
}

@Injectable({
  providedIn: 'root'
})
export class MetadataService {
  private http = inject(HttpClient);
  private baseUrl = '/api/metadata';

  getPage(page: number = 1, pageSize: number = 10, fileType?: number): Observable<PagedResult> {
    let url = `${this.baseUrl}/page?page=${page}&pageSize=${pageSize}`;
    if (fileType !== undefined) {
      url += `&fileType=${fileType}`;
    }
    return this.http.get<PagedResult>(url);
  }

  getById(id: string): Observable<Metadata> {
    return this.http.get<Metadata>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateMetadataRequest): Observable<Metadata> {
    return this.http.post<Metadata>(this.baseUrl, request);
  }

  update(id: string, request: UpdateMetadataRequest): Observable<Metadata> {
    return this.http.put<Metadata>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
