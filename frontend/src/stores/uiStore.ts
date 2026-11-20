import { create } from 'zustand';

interface UiState {
  isCartDrawerOpen: boolean;
  isMobileMenuOpen: boolean;
  isFilterSheetOpen: boolean;
  openCartDrawer: () => void;
  closeCartDrawer: () => void;
  toggleCartDrawer: () => void;
  openMobileMenu: () => void;
  closeMobileMenu: () => void;
  toggleMobileMenu: () => void;
  openFilterSheet: () => void;
  closeFilterSheet: () => void;
  toggleFilterSheet: () => void;
}

export const useUiStore = create<UiState>((set) => ({
  isCartDrawerOpen: false,
  isMobileMenuOpen: false,
  isFilterSheetOpen: false,
  openCartDrawer: () => set({ isCartDrawerOpen: true }),
  closeCartDrawer: () => set({ isCartDrawerOpen: false }),
  toggleCartDrawer: () => set((s) => ({ isCartDrawerOpen: !s.isCartDrawerOpen })),
  openMobileMenu: () => set({ isMobileMenuOpen: true }),
  closeMobileMenu: () => set({ isMobileMenuOpen: false }),
  toggleMobileMenu: () => set((s) => ({ isMobileMenuOpen: !s.isMobileMenuOpen })),
  openFilterSheet: () => set({ isFilterSheetOpen: true }),
  closeFilterSheet: () => set({ isFilterSheetOpen: false }),
  toggleFilterSheet: () => set((s) => ({ isFilterSheetOpen: !s.isFilterSheetOpen })),
}));
