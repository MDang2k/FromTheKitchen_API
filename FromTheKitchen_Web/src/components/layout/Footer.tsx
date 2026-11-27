function Footer() {
  return (
    <footer className="bg-ink py-8 text-cream">
      <div className="container mx-auto px-4">
        <p className="text-center">
          &copy; {new Date().getFullYear()} From The Kitchen. All rights reserved.
        </p>
      </div>
    </footer>
  );
}

export default Footer;