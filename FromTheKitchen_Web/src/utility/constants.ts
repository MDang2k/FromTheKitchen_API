// In production the API serves this app, so calls go to the same origin ("").
// In development Vite runs on :5173 and the API on :7145.
export const API_BASE_URL: string =
    import.meta.env.VITE_API_URL ?? (import.meta.env.DEV ? "https://localhost:7145" : "");

export interface MenuItem {
    id: number;
    name: string;
    description?: string;
    category: string;
    specialTag?: string;
    price: number;
    image: string;
}

// Must match SD.status_* in the API.
export const ORDER_STATUS = {
    CONFIRMED: "Confirmed",
    READY: "Ready For Pick Up",
    COMPLETED: "Completed",
    CANCELLED: "Cancelled",
} as const;

export interface OrderLine {
    orderDetailId: number;
    itemName: string;
    quantity: number;
    price: number;
}

export interface Order {
    orderHeaderId: number;
    orderDate: string;
    status: string;
    orderTotal: number;
    totalItem: number;
    pickUpName: string;
    phoneNumber: string;
    pickUpEmail: string;
    orderDetails: OrderLine[];
}

export const ROUTES = {
    HOME: "/",
    LOGIN: "/login",
    REGISTER: "/register",
    CART: "/cart",
    CHECKOUT: "/checkout",
    ORDER_CONFIRMATION: "/order-confirmation",
    MY_ORDERS: "/my-orders",
    MENU_MANAGEMENT: "/menu-management",
    ORDER_MANAGEMENT: "/order-management",
}