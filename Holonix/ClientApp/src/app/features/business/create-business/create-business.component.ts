import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { BusinessService, BusinessServiceOption } from '../../../core/services/business.service';

@Component({
  selector: 'app-create-business',
  templateUrl: './create-business.component.html',
  styleUrls: ['./create-business.component.css'],
})
export class CreateBusinessComponent implements OnInit {
  submitAttempted = false;
  serviceSearch = '';
  allServices: BusinessServiceOption[] = [];
  selectedServices: BusinessServiceOption[] = [];
  serviceDropdownOpen = false;
  loadingServices = false;

  businessForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    address1: ['', [Validators.required, Validators.maxLength(200)]],
    address2: ['', [Validators.maxLength(200)]],
    city: ['', [Validators.required, Validators.maxLength(120)]],
    state: ['', [Validators.required, Validators.maxLength(120)]],
    zipCode: ['', [Validators.required, Validators.maxLength(32)]],
    country: ['United States', [Validators.required, Validators.maxLength(120)]],
  });

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    private readonly businessService: BusinessService
  ) {}

  ngOnInit(): void {
    if (!localStorage.getItem('holonix_token')) {
      this.router.navigate(['/login']);
      return;
    }

    this.loadingServices = true;
    this.businessService.getServices().subscribe({
      next: (services) => {
        this.allServices = services;
        this.loadingServices = false;
      },
      error: () => {
        this.loadingServices = false;
        this.snackBar.open('Could not load business services.', 'Close', {
          duration: 3000,
          panelClass: ['snack-error'],
        });
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/profile']);
  }

  get filteredServices(): BusinessServiceOption[] {
    const query = this.serviceSearch.trim().toLowerCase();
    return this.allServices
      .filter((service) => !this.selectedServices.some((selected) => selected.serviceId === service.serviceId))
      .filter((service) => !query || service.name.toLowerCase().includes(query))
      .slice(0, 10);
  }

  onServiceSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.serviceSearch = input.value;
    this.serviceDropdownOpen = true;
  }

  openServiceDropdown(): void {
    this.serviceDropdownOpen = true;
  }

  closeServiceDropdown(): void {
    window.setTimeout(() => {
      this.serviceDropdownOpen = false;
    }, 120);
  }

  selectService(service: BusinessServiceOption): void {
    if (this.selectedServices.some((selected) => selected.serviceId === service.serviceId)) {
      return;
    }

    this.selectedServices = [...this.selectedServices, service];
    this.serviceSearch = '';
    this.serviceDropdownOpen = false;
  }

  removeService(serviceId: number): void {
    this.selectedServices = this.selectedServices.filter((service) => service.serviceId !== serviceId);
  }

  submit(): void {
    this.submitAttempted = true;

    if (this.businessForm.invalid) {
      this.businessForm.markAllAsTouched();
      return;
    }

    if (this.selectedServices.length === 0) {
      this.serviceDropdownOpen = true;
      this.snackBar.open('Select at least one service to continue.', 'Close', {
        duration: 3000,
        panelClass: ['snack-error'],
      });
      return;
    }

    this.snackBar.open('Business creation flow is ready for backend wiring.', 'Close', {
      duration: 3000,
      panelClass: ['snack-success'],
    });
  }
}
