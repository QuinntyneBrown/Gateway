import { Routes } from '@angular/router';
import { MetadataListComponent } from './components/metadata-list/metadata-list.component';
import { MetadataDetailComponent } from './components/metadata-detail/metadata-detail.component';

export const routes: Routes = [
  { path: '', redirectTo: '/metadata', pathMatch: 'full' },
  { path: 'metadata', component: MetadataListComponent },
  { path: 'metadata/create', component: MetadataDetailComponent },
  { path: 'metadata/:id', component: MetadataDetailComponent },
  { path: 'metadata/:id/edit', component: MetadataDetailComponent }
];
